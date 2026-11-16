"""Download the selected public HiRISE products outside Unity's Assets folder."""
from pathlib import Path
import argparse
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
BASE = ("https://www.uahirise.org/PDS/DTM/ESP/ORB_024200_024299/"
        "ESP_024234_1755_ESP_024300_1755/")
PRODUCTS = [
    "DTEEC_024234_1755_024300_1755_U01.IMG",
    "ESP_024234_1755_RED_C_01_ORTHO.JP2",
    "ESP_024234_1755_RED_C_01_ORTHO.LBL",
]
JEZERO_BASE = ("https://www.uahirise.org/PDS/DTM/ESP/ORB_045900_045999/"
               "ESP_045994_1985_ESP_046060_1985/")
JEZERO_PRODUCTS = [
    "DTEEC_045994_1985_046060_1985_U01.IMG",
    "ESP_045994_1985_RED_C_01_ORTHO.JP2",
    "ESP_045994_1985_RED_C_01_ORTHO.LBL",
]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--region", choices=["Gale", "Jezero"], default="Gale")
    args = parser.parse_args()
    directory = ROOT / "TerrainSources/Original" / args.region
    base, products = (JEZERO_BASE, JEZERO_PRODUCTS) if args.region == "Jezero" else (BASE, PRODUCTS)
    directory.mkdir(parents=True, exist_ok=True)
    for name in products:
        target = directory / name
        if target.exists():
            print(f"Already downloaded: {name} ({target.stat().st_size} bytes)", flush=True)
            continue
        temporary = target.with_suffix(target.suffix + ".part")
        print(f"Downloading {base + name}", flush=True)
        with urllib.request.urlopen(base + name, timeout=120) as response:
            with temporary.open("wb") as output:
                while block := response.read(1024 * 1024):
                    output.write(block)
        temporary.replace(target)
        print(f"Saved {name} ({target.stat().st_size} bytes)", flush=True)


if __name__ == "__main__":
    main()
