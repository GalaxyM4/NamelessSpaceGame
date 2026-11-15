"""Run with: uv run --python 3.13 --with rasterio --with pillow python -m unittest discover -s Tools/HiRISE"""
import tempfile
import unittest
from pathlib import Path

import numpy as np
import rasterio
from rasterio.transform import from_origin

from prepare import MARS_SHADOW, MARS_HIGHLIGHT, sample, tint_imagery, validate_heights


class PreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "source.tif"

    def write(self, heights):
        with rasterio.open(self.path, "w", driver="GTiff", height=heights.shape[0],
                           width=heights.shape[1], count=1, dtype="float32",
                           crs="EPSG:32631", transform=from_origin(0, 10, 1, 1),
                           nodata=-9999) as dst:
            dst.write(heights.astype("float32"), 1)

    def test_pixel_centers_and_unity_orientation(self):
        # South rows have larger elevations; eastward columns increase by one.
        original = np.arange(100, dtype="float32").reshape(10, 10)
        self.write(original)
        with rasterio.open(self.path) as src:
            heights = sample(src, (0.5, 0.5, 9.5, 9.5), 10)
        np.testing.assert_allclose(heights, original)
        unity = np.flipud(heights).astype("<f4")
        self.assertEqual(float(unity[0, 0]), 90)  # Southwest.
        self.assertEqual(float(unity[-1, -1]), 9)  # Northeast.
        self.assertEqual(unity.tobytes()[:4], b"\x00\x00\xb4\x42")

    def test_missing_data_rejected(self):
        heights = np.ones((10, 10), dtype="float32")
        heights[5, 5] = -9999
        self.write(heights)
        with rasterio.open(self.path) as src:
            prepared = sample(src, (0.5, 0.5, 9.5, 9.5), 10)
        with self.assertRaises(ValueError):
            validate_heights(prepared)

    def test_invalid_elevation_rejected(self):
        for invalid in (np.nan, np.inf, -3.402822655e38):
            with self.assertRaises(ValueError):
                validate_heights(np.array([[invalid]], dtype="float32"))

    def test_tint_preserves_layout_and_brightness_order(self):
        grayscale = np.arange(256, dtype="uint8").reshape(16, 16)
        original = grayscale.copy()
        tinted = tint_imagery(grayscale)
        self.assertEqual(tinted.shape, (16, 16, 3))
        np.testing.assert_array_equal(grayscale, original)
        np.testing.assert_array_equal(tinted[0, 0], MARS_SHADOW)
        np.testing.assert_array_equal(tinted[-1, -1], MARS_HIGHLIGHT)
        self.assertTrue(np.all(np.diff(tinted.reshape(-1, 3).astype(int), axis=0) >= 0))
        self.assertTrue(np.all(tinted[..., 0] > tinted[..., 1]))
        self.assertTrue(np.all(tinted[..., 1] > tinted[..., 2]))


if __name__ == "__main__":
    unittest.main()
