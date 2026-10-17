"""Retire Cultural objects after moving every live record to Salvage."""
import move_camp_items_to_salvage as migration


if __name__ == '__main__':
    migration.OUT = migration.batch.migration.ROOT / 'Logs/CulturalItemsToSalvage20261016'
    migration.AUDIT = migration.batch.migration.ROOT / 'Docs/Design/Items/CULTURAL_ITEMS_SALVAGE_CLASSIFICATION_2026-10-16.json'
    migration.SOURCE_CATEGORY = 'Cultural objects'
    migration.REASON = 'Author direction October 16: all remaining Cultural objects are Salvage; remove the cultural items section. Preserve item identity, descriptions, condition, tiers, uses and recipe semantics.'
    migration.run()
