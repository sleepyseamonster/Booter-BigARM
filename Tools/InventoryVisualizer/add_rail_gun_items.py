"""Add the author-requested rail gun and missing catalog-specific items."""
import add_arc_power_armor as batch

if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/RailGunItems20261016'
    batch.SPEC = 'Docs/Design/Items/RAIL_GUN_ITEMS_2026-10-16.json'
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
