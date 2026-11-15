"""Download the selected public HiRISE products outside Unity's Assets folder."""
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
BASE = ("https://www.uahirise.org/PDS/DTM/ESP/ORB_024200_024299/"
        "ESP_024234_1755_ESP_024300_1755/")
PRODUCTS = [
    "DTEEC_024234_1755_024300_1755_U01.IMG",
    "ESP_024234_1755_RED_C_01_ORTHO.JP2",
    "ESP_024234_1755_RED_C_01_ORTHO.LBL",
]


def main():
    directory = ROOT / "TerrainSources/Original/Gale"
    directory.mkdir(parents=True, exist_ok=True)
    for name in PRODUCTS:
        target = directory / name
        if target.exists():
            print(f"Already downloaded: {name} ({target.stat().st_size} bytes)", flush=True)
            continue
        temporary = target.with_suffix(target.suffix + ".part")
        print(f"Downloading {BASE + name}", flush=True)
        with urllib.request.urlopen(BASE + name, timeout=120) as response:
            with temporary.open("wb") as output:
                while block := response.read(1024 * 1024):
                    output.write(block)
        temporary.replace(target)
        print(f"Saved {name} ({target.stat().st_size} bytes)", flush=True)


if __name__ == "__main__":
    main()
