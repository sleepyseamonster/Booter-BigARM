"""New-side color constraints with a measured, single-pixel corner exception."""
import numpy as np

def feather_color(step, matched, south, west=None, audited_corner_difference=None):
    if type(step) is not int or not 1 <= step <= 6 or matched.shape != (2048, 2048, 3) or south.shape != (2048, 3):
        raise ValueError("Invalid second-row color contract")
    if not np.isfinite(matched).all():
        raise ValueError("Invalid color samples")
    if step == 1:
        if west is not None or audited_corner_difference is not None:
            raise ValueError("The first section has no west anchor or corner conflict")
        difference = None
    else:
        if west is None or west.shape != (2048, 3) or audited_corner_difference is None:
            raise ValueError("Later sections require west anchor and fresh measured corner audit")
        difference = np.abs(south[0].astype("int16") - west[-1].astype("int16"))
        if not np.array_equal(difference, np.asarray(audited_corner_difference)):
            raise ValueError("Changed or unaudited retained corner values")
    result = matched.astype("float32").copy()
    strip = np.clip(1 - np.arange(2048) / 128, 0, 1).astype("float32")
    if west is not None:
        result += (west - result[:, 0])[:, None, :] * strip[None, :, None]
    result += (south - result[-1])[None, :, :] * strip[::-1, None, None]
    result = np.rint(np.clip(result, 0, 255)).astype("uint8")
    if west is not None:
        result[:-1, 0] = west[:-1]
    result[-1] = south
    if not np.array_equal(result[-1], south) or (west is not None and not np.array_equal(result[:-1, 0], west[:-1])):
        raise ValueError("Color anchor preservation failed")
    return result, {"west_anchor_present": west is not None, "southwest_difference_rgb": None if difference is None else difference.tolist(), "west_exception_pixels": 0 if difference is None else int(np.any(difference)), "priority": "south row; west column everywhere except sole southwest raster-cell center", "feather_pixels": 128, "feather_m": 256}
