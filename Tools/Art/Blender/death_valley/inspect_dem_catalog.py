"""Record USGS downloadable DEM products intersecting a projected study area."""

from __future__ import annotations

import argparse
import json
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

from pyproj import Transformer


API = "https://tnmaccess.nationalmap.gov/api/v1/products"
DATASETS = {
    "one_third_arc_second": "National Elevation Dataset (NED) 1/3 arc-second",
    "one_meter": "Digital Elevation Model (DEM) 1 meter",
}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--corridor", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    record = json.loads(args.corridor.read_text())
    if record["working_crs"] != "EPSG:26911":
        raise ValueError("Expected EPSG:26911 corridor")
    x0, y0, x1, y1 = record["bounds_m"]
    transform = Transformer.from_crs("EPSG:26911", "EPSG:4326", always_xy=True)
    corners = [transform.transform(x, y) for x in (x0, x1) for y in (y0, y1)]
    bbox = [min(p[0] for p in corners), min(p[1] for p in corners),
            max(p[0] for p in corners), max(p[1] for p in corners)]
    results = {}
    for key, dataset in DATASETS.items():
        params = {"bbox": ",".join(str(value) for value in bbox), "datasets": dataset, "max": 1000}
        url = f"{API}?{urllib.parse.urlencode(params)}"
        request = urllib.request.Request(url, headers={"User-Agent": "BooterBigARM-DeathValleyStudy/1.0"})
        with urllib.request.urlopen(request, timeout=90) as response:
            data = json.load(response)
        if data.get("errors"):
            raise RuntimeError(f"TNM product query failed: {data['errors']}")
        results[key] = {"request": params, "total": data["total"], "items": data["items"]}
    output = {
        "acquired_utc": datetime.now(timezone.utc).isoformat(),
        "source": API,
        "corridor_bounds_epsg26911": record["bounds_m"],
        "query_bbox_epsg4326": bbox,
        "datasets": results,
        "note": "Catalog intersection is evidence of candidate products, not proof that every location has 1 m data; use product footprints and metadata before asserting local resolution.",
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + "\n")
    print(json.dumps({key: value["total"] for key, value in results.items()}))


if __name__ == "__main__":
    main()
