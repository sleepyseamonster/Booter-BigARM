"""Append ten new ancient industrial-recovery proposals without changing old items."""
import add_arc_power_armor as batch


if __name__=='__main__':
    batch.OUT=batch.append.ROOT/'Logs/TenAncientIndustrialFinds20261016'
    batch.SPEC='Docs/Design/Items/TEN_ANCIENT_INDUSTRIAL_FINDS_2026-10-16.json'
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
