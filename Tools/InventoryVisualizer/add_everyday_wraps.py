"""Refine generic wraps into one garment and add four separate companion pieces."""
import json
import add_five_clothing_families as batch


def configure():
    batch.OUT=batch.ROOT/'Logs/EverydayWraps20261016'
    batch.DATA=batch.ROOT/'Docs/Design/Items/EVERYDAY_WRAPS_2026-10-16.json'
    batch.REUSE={'torso':'proposal.gear.simple_wraps'}
    spec=json.loads(batch.DATA.read_text(encoding='utf-8'))
    batch.specification=lambda: spec


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
