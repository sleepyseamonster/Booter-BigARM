"""Compare matched Legger before/after orthographic reviews (Pillow + NumPy)."""
import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('evidence', type=Path)
    parser.add_argument('--before-label', default='source')
    parser.add_argument('--after-label', default='candidate')
    args = parser.parse_args()
    metrics = {}
    sheet = Image.new('RGB', (1280, 4*670), '#262626')
    draw = ImageDraw.Draw(sheet)
    for row, view in enumerate(['front', 'side', 'elevated', 'underside']):
        for mode in ['texture', 'single']:
            before = Image.open(args.evidence/f'before_{view}_{mode}.png').convert('RGBA')
            after = Image.open(args.evidence/f'after_{view}_{mode}.png').convert('RGBA')
            a, b = np.asarray(before), np.asarray(after)
            ma, mb = a[:,:,3] > 127, b[:,:,3] > 127
            intersection, union = ma & mb, ma | mb
            delta = np.abs(a[:,:,:3].astype(float)-b[:,:,:3].astype(float))
            metrics[f'{view}_{mode}'] = {
                'silhouette_iou': float(intersection.sum()/union.sum()),
                'silhouette_disagreement_pixels': int((ma ^ mb).sum()),
                'mean_rgb_difference_on_overlap_0_255': float(delta[intersection].mean())}
            if mode == 'texture':
                for col, im in enumerate([before, after]):
                    im.thumbnail((640, 640))
                    canvas = Image.new('RGB', im.size, '#262626')
                    canvas.paste(im, mask=im.getchannel('A'))
                    sheet.paste(canvas, (col*640, row*670+30))
                    draw.text((col*640+12, row*670+8),
                              f'{view} / {args.before_label if col == 0 else args.after_label}', fill='white')
    (args.evidence/'image_comparison.json').write_text(json.dumps(metrics, indent=2)+'\n')
    sheet.save(args.evidence/'comparison.png')
    print(json.dumps(metrics, indent=2))


if __name__ == '__main__':
    main()
