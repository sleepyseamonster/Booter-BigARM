"""Add four standalone shell-and-metal camp goods with the preserving appender."""
import add_arc_power_armor as batch

if __name__=='__main__':
    batch.OUT=batch.append.ROOT/'Logs/FourCampItems20261016'
    batch.SPEC='Docs/Design/Items/FOUR_CAMP_ITEMS_2026-10-16.json'
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
