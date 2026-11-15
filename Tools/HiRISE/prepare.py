# /// script
# requires-python = ">=3.11"
# dependencies = ["rasterio==1.5.2", "numpy==2.4.6", "pillow==12.2.0"]
# ///
"""Prepare a georeferenced HiRISE crop; never derive heights from image brightness."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image
from rasterio.enums import Resampling
from rasterio.transform import from_bounds
from rasterio.warp import reproject
from rasterio.windows import from_bounds as window_from_bounds

ROOT = Path(__file__).resolve().parents[2]
DTM = "DTEEC_024234_1755_024300_1755_U01"
ORTHO = "ESP_024234_1755_RED_C_01_ORTHO"
SOURCE_PAGE = "https://www.uahirise.org/dtm/ESP_024234_1755"
# Artistic sRGB palette inspired by the original warm rock texture.
# A strictly increasing ramp retains the orthoimage's spatial patterns.
MARS_SHADOW = (54, 35, 23)
MARS_HIGHLIGHT = (218, 166, 119)


def tint_imagery(grayscale):
    """Colorize brightness only; no resampling, texture overlay or height changes."""
    shadow = np.asarray(MARS_SHADOW, dtype="float32")
    highlight = np.asarray(MARS_HIGHLIGHT, dtype="float32")
    brightness = grayscale.astype("float32")[..., None] / 255
    return np.round(shadow + brightness * (highlight - shadow)).astype("uint8")


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            h.update(block)
    return h.hexdigest()


def sample(dataset, bounds, resolution):
    """Sample vertices including bounds' endpoints, not pixel-edge positions."""
    left, bottom, right, top = bounds
    spacing = (right - left) / (resolution - 1)
    grid = from_bounds(left - spacing / 2, bottom - spacing / 2,
                       right + spacing / 2, top + spacing / 2, resolution, resolution)
    result = np.full((resolution, resolution), np.nan, dtype="float32")
    reproject(
        source=rasterio.band(dataset, 1), destination=result,
        src_transform=dataset.transform, src_crs=dataset.crs,
        src_nodata=dataset.nodata, dst_transform=grid, dst_crs=dataset.crs,
        dst_nodata=np.nan, resampling=Resampling.bilinear,
    )
    return result


def validate_heights(heights):
    if not np.isfinite(heights).all() or np.any(np.abs(heights) > 100000):
        raise ValueError("Crop contains missing or invalid elevations. Select another crop.")


def select_crop(dataset, extent):
    """Screen real, fully covered crops; score low slopes with some visible relief."""
    candidates = []
    left, bottom, right, top = dataset.bounds
    half = extent / 2
    # Screen a coarse grid, then fully validate the winning candidate.
    for cy in np.linspace(bottom + half + 4, top - half - 4, 13):
        for cx in np.linspace(left + half + 4, right - half - 4, 9):
            bounds = (cx - half, cy - half, cx + half, cy + half)
            window = window_from_bounds(*bounds, transform=dataset.transform)
            h = dataset.read(1, window=window, out_shape=(129, 129), masked=True,
                             resampling=Resampling.bilinear)
            if np.ma.getmaskarray(h).any():
                continue
            h = np.asarray(h)
            if not np.isfinite(h).all() or np.any(np.abs(h) > 100000):
                continue
            dz, dx = np.gradient(h, extent / 128)
            slopes = np.degrees(np.arctan(np.hypot(dx, dz)))
            relief = float(np.ptp(h))
            score = float(np.percentile(slopes, 90)) + abs(relief - 45) * 0.025
            candidates.append((score, bounds))
    for _, bounds in sorted(candidates):
        h = sample(dataset, bounds, 2049)
        try:
            validate_heights(h)
            return bounds, h
        except ValueError:
            continue
    raise ValueError("No fully valid crop found. Choose a smaller extent or explicit bounds.")


def prepare(args):
    original = ROOT / "TerrainSources/Original/Gale"
    prepared = ROOT / "TerrainSources/Prepared/Gale"
    textures = ROOT / "Assets/Textures/HiRISE"
    dtm_path = original / f"{DTM}.IMG"
    ortho_path = original / f"{ORTHO}.JP2"
    with rasterio.open(dtm_path) as dtm, rasterio.open(ortho_path) as ortho:
        print(json.dumps({
            "dtm": {"size": dtm.shape, "bounds": list(dtm.bounds),
                    "transform": list(dtm.transform), "nodata": dtm.nodata,
                    "crs": str(dtm.crs)},
            "ortho": {"size": ortho.shape, "bounds": list(ortho.bounds),
                      "crs": str(ortho.crs), "dtype": ortho.dtypes},
        }, indent=2), flush=True)
        if args.inspect:
            return
        if dtm.crs is None or ortho.crs is None:
            raise ValueError("Both source products require georeferencing; check companion labels.")
        if dtm.crs.linear_units != "metre":
            raise ValueError("DTM must use a projected coordinate system measured in metres.")
        if args.center:
            cx, cy = args.center
            half = args.extent / 2
            bounds = (cx - half, cy - half, cx + half, cy + half)
            heights = sample(dtm, bounds, 2049)
        else:
            bounds, heights = select_crop(dtm, args.extent)
        validate_heights(heights)
        image = np.full((args.texture_size, args.texture_size), np.nan, dtype="float32")
        reproject(
            source=rasterio.band(ortho, 1), destination=image,
            src_transform=ortho.transform, src_crs=ortho.crs,
            # This product's PDS label declares CORE_NULL=0; some JP2 drivers
            # omit the detached label's nodata value.
            src_nodata=ortho.nodata if ortho.nodata is not None else 0,
            dst_transform=from_bounds(*bounds, args.texture_size, args.texture_size),
            dst_crs=dtm.crs, dst_nodata=np.nan, resampling=Resampling.bilinear,
        )
        if not np.isfinite(image).all():
            raise ValueError("Matching orthoimage does not fully cover the crop.")
        low, high = np.percentile(image, [0.5, 99.5])
        if high <= low:
            raise ValueError("Orthoimage has no usable contrast.")
        grayscale = np.round(np.clip((image - low) / (high - low), 0, 1) * 255).astype("uint8")
        prepared.mkdir(parents=True, exist_ok=True)
        textures.mkdir(parents=True, exist_ok=True)
        height_path = prepared / "heights.f32"
        # Unity indexes [z,x]: first row south, +X east, +Z north.
        np.flipud(heights).astype("<f4").tofile(height_path)
        texture_path = textures / "GaleOrtho.png"
        grayscale_path = prepared / "ortho-monochrome.png"
        Image.fromarray(grayscale).save(grayscale_path)
        # Stage outside Assets so a live Editor cannot import a partially written PNG.
        staged_texture = prepared / "ortho-color.tmp.png"
        Image.fromarray(tint_imagery(grayscale)).save(staged_texture)
        staged_texture.replace(texture_path)
        dz, dx = np.gradient(heights, args.extent / 2048)
        slopes = np.degrees(np.arctan(np.hypot(dx, dz)))
        metadata = {
            "schemaVersion": 1, "region": "Gale MSL landing-site region",
            "sourcePage": SOURCE_PAGE, "dtmProduct": DTM, "orthoProduct": ORTHO,
            "credit": "NASA/JPL-Caltech/University of Arizona; DTM produced by USGS",
            "heightFile": "heights.f32", "heightSha256": digest(height_path),
            "textureAssetPath": texture_path.relative_to(ROOT).as_posix(),
            "textureSha256": digest(texture_path),
            "resolution": 2049, "sizeMeters": args.extent,
            "minimumElevationMeters": float(heights.min()),
            "maximumElevationMeters": float(heights.max()),
            "projectedBounds": list(bounds), "crsWkt": dtm.crs.to_wkt(),
            "orientation": "+X=east,+Z=north; float32 LE rows south-to-north",
            "imagery": "RED monochrome; percentile contrast stretch and artistic Mars tint, not true color",
            "monochromeFile": grayscale_path.name,
            "monochromeSha256": digest(grayscale_path),
            "artisticPalette": {"shadowSrgb8": MARS_SHADOW, "highlightSrgb8": MARS_HIGHLIGHT},
            "imageContrastMinimum": float(low), "imageContrastMaximum": float(high),
            "slopeMedianDegrees": float(np.median(slopes)),
            "slope90Degrees": float(np.percentile(slopes, 90)),
            "sourceDtmSha256": digest(dtm_path), "sourceOrthoSha256": digest(ortho_path),
        }
        (prepared / "manifest.json").write_text(json.dumps(metadata, indent=2) + "\n")
        print(json.dumps(metadata, indent=2), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inspect", action="store_true")
    parser.add_argument("--extent", type=float, default=2048.0)
    parser.add_argument("--center", type=float, nargs=2, metavar=("EASTING", "NORTHING"))
    parser.add_argument("--texture-size", type=int, default=4096)
    args = parser.parse_args()
    if not 0 < args.extent <= 10000 or not 256 <= args.texture_size <= 8192:
        parser.error("extent must be (0,10000] metres; texture-size must be 256..8192")
    prepare(args)
