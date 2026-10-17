"""Add one iron ring puzzle cultural-object proposal through the preserving appender."""
import add_arc_power_armor as batch

if __name__=='__main__':
    batch.OUT=batch.append.ROOT/'Logs/IronRingPuzzle20261016'
    batch.SPEC='Docs/Design/Items/IRON_RING_PUZZLE_2026-10-16.json'
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
