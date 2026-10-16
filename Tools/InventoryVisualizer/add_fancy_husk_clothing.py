"""Refine the broad husk-fashion entry and add four individual companion garments."""
import json
import add_five_clothing_families as batch


def configure():
    batch.OUT=batch.ROOT/'Logs/FancyHuskClothing20261016'
    batch.DATA=batch.ROOT/'Docs/Design/Items/FANCY_HUSK_FIBER_CLOTHING_2026-10-16.json'
    batch.REUSE={'coat':'proposal.fashion.husk_garment'}
    spec=json.loads(batch.DATA.read_text(encoding='utf-8'))
    batch.specification=lambda: spec


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
