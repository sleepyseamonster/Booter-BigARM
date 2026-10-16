"""Add five as-found ancient components with the preserving catalog appender."""
import add_arc_power_armor as batch

if __name__=='__main__':
    batch.OUT=batch.append.ROOT/'Logs/FiveAncientFinds20261016'
    batch.SPEC='Docs/Design/Items/FIVE_ANCIENT_FINDS_2026-10-16.json'
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
