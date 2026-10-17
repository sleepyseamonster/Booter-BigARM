"""Move all remaining recovered mechanisms to Salvage and retire their section."""
import retire_field_supplies as batch


def configure():
    batch.OUT=batch.migration.ROOT/'Logs/RecoveredMechanismsRetirement20261015'
    batch.AUDIT=batch.migration.ROOT/'Docs/Design/Items/RECOVERED_MECHANISMS_SALVAGE_CLASSIFICATION.json'
    batch.OLD='Recovered mechanisms'
    batch.TARGET='Salvage'
    batch.REASON='Author direction October 15: all Recovered mechanisms entries belong in Salvage; retire the separate Recovered mechanisms section. Identity, condition, tiers, properties and recipe roles remain unchanged.'


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
