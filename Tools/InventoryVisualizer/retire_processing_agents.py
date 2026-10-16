"""Merge processing agents into Crafting components and retire the section."""
import retire_field_supplies as batch


def configure():
    batch.OUT=batch.migration.ROOT/'Logs/ProcessingAgentsRetirement20261015'
    batch.AUDIT=batch.migration.ROOT/'Docs/Design/Items/PROCESSING_AGENTS_COMPONENT_CLASSIFICATION.json'
    batch.OLD='Processing agents'
    batch.TARGET='Crafting components'
    batch.REASON='Author direction October 15: all Processing agents belong in Crafting components; retire the separate Processing agents section. Identity, condition, tiers, properties and recipe roles remain unchanged.'


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
