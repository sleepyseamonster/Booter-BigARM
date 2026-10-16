"""Move the remaining general Attachments to Armor; retain Legger categories."""
import json
from collections import Counter
import retire_field_supplies as batch


def configure():
    batch.OUT=batch.migration.ROOT/'Logs/AttachmentsArmor20261016'
    batch.AUDIT=batch.migration.ROOT/'Docs/Design/Items/ATTACHMENTS_ARMOR_CLASSIFICATION.json'
    batch.OLD='Attachments'
    batch.TARGET='Armor'
    batch.REASON='Author direction October 16: all remaining general Attachments belong in Armor; retire the empty Attachments section. Category assignment preserves item type, properties and recipe roles, including the tool belt. Legger attachments remains separate.'


def update_manifest():
    audit=json.loads(batch.AUDIT.read_text(encoding='utf-8'))
    path=batch.migration.ROOT/'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
    original=path.read_bytes();manifest=json.loads(original)
    selected={i['id'] for i in audit['items']}
    for item in manifest['items']:
        if item['id'] in selected:item.update(category='Armor',reason=batch.REASON)
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']))
    manifest['sourceHashAfter']=audit['sourceHashAfter']
    manifest.setdefault('revisions',[]).append(dict(reason=batch.REASON,movedIds=sorted(selected),sourceHashBefore=audit['sourceHashBefore'],sourceHashAfter=audit['sourceHashAfter']))
    batch.atomic_write(path,(json.dumps(manifest,indent=2)+'\n').encode(),batch.digest(original))


if __name__=='__main__':
    configure()
    batch.guard.OUT=batch.OUT;batch.guard.prepare=batch.prepare;batch.guard.apply=batch.apply
    batch.guard.run()
    update_manifest()
