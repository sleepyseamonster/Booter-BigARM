"""Expand Biological resources to 100 through reviewed inputs and a preserving merge."""
import json
from collections import Counter
from io import BytesIO
from zipfile import ZipFile

import openpyxl
import add_ancient_salvage as append
import organize_camp_items as guard
from assign_rarity_baseline import merge as merge_rarities, MAPPING
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import atomic_write, digest, master_items, parse_sheets

ROOT = append.ROOT
SOURCE = append.SOURCE
OUT = ROOT / 'Logs/MegafaunaBiology20261016'
SPEC = ROOT / 'Docs/Design/Items/MEGAFAUNA_BIOLOGICAL_ITEMS_2026-10-16.json'
AUDIT = ROOT / 'Docs/Design/Items/MEGAFAUNA_BIOLOGICAL_EXPANSION_2026-10-16.json'

# slug, name, tier, material, intended use, form, source anatomy, purpose, value basis
PROPOSALS = [
('thorax_scutes','Megafauna thorax scute set',2,'Shell / chitin','Protective fabrication','Shell plate','Armored thorax; retain overlapping edges as a matched set',
 'A matched set of interlocking thorax scutes with their overlap geometry intact. Candidate facing for segmented guards, distinct from loose bulk shell husks.',
 'Matched curvature and intact overlap edges are scarce specialist blanks; ordinary broken shell remains low-value bulk.'),
('shoulder_buttress','Megafauna shoulder buttress',2,'Faunal hard tissue','Binding and construction','Solid piece','Forelimb shoulder junction; recover the internal branching support',
 'A branching internal support from a heavy forelimb attachment. Its broad roots offer candidate load-spreading mounting blanks rather than flat armor plates.',
 'Deep extraction and retaining the branched load path make this more valuable than surface shell.'),
('tarsal_pad_stack','Megafauna tarsal pad stack',2,'Faunal tissue','Traversal','Assembly','Weight-bearing foot; preserve the layered contact pad',
 'A layered foot-contact pad retained with its supporting cushion. Candidate traction and compliant-contact material, distinct from loose gripping-pad scales.',
 'Complete pad layers are harder to recover than detached scales and offer a larger specialist workpiece.'),
('excavator_wedge','Megafauna excavator wedge',2,'Faunal hard tissue','Excavation','Solid piece','Burrowing forelimb; remove the broad leading wedge',
 'The broad leading wedge of a giant digging limb. Its thick working face is a candidate excavation-tool blank after inspection and mounting.',
 'An intact broad working face has specialist value; worn or fractured pieces revert to ordinary hard-tissue feed.'),
('mandible_ridges','Megafauna crushing ridges',2,'Biological and mineral mixture','Tool fabrication','Strip','Crushing mandible; separate the dense opposing ridge',
 'Dense ridged material from an opposing crushing jaw surface. Candidate gripping or crushing inserts, with a different working profile from a flat jaw plate.',
 'Retaining a continuous ridge and useful contact profile demands selective harvest from a dangerous source.'),
('rasp_bands','Megafauna burrowing rasp bands',2,'Faunal hard tissue','Surface finishing','Strip','Burrowing limb; recover curved toothed abrasion bands',
 'Curved bands of closely spaced mineral-scouring teeth. Candidate coarse abrasion inserts for shaped surfaces, distinct from loose grinding grit.',
 'A continuous patterned tooth band is a specialist blank with a low intact recovery rate.'),
('nuchal_cable','Megafauna nuchal tension cable',2,'Faunal tissue','Binding and construction','Bundle','Head-support junction; preserve a long aligned tension bundle',
 'An unusually long aligned bundle from a giant head-support junction. Candidate heavy binding stock after stabilization, rather than ordinary short dried tendon.',
 'Length, alignment and damage-free extraction create value beyond common mixed tendon bundles.'),
('flight_collar','Megafauna flight-hinge collar',2,'Faunal hard tissue','Equipment mounting','Solid part','Large flyer wing root; retain the ring-shaped bearing surround',
 'A ring-shaped wing-root bearing surround with its broad attachment flange intact. Candidate fitted mounting stock; it is not a complete wing or a working bearing.',
 'The continuous flange and bore geometry are uncommon specialist shapes lost when a joint is broken apart.'),
('spiracle_leaves','Megafauna spiracle valve leaves',2,'Faunal membrane','Dust exclusion','Membrane','Breathing opening; preserve the overlapping closure leaves',
 'Thin overlapping closure leaves from a large breathing opening. Candidate flexible grit-shutter material, separate from rigid breathing-filter plates.',
 'Usable matched leaf sets require careful extraction and are less replaceable than ordinary filter debris.'),
('bellows_pleats','Megafauna abdominal bellows pleats',2,'Faunal membrane','Liquid and bulk containment','Membrane','Abdominal air sac; recover an unbroken concertina-fold section',
 'A folded membrane section that expands through its existing pleat geometry. Candidate flexible chamber or bellows feed after compatibility checks.',
 'An intact pleated span is a specialist harvest whose folds are easily torn during a fight or extraction.'),
('resonator_diaphragms','Megafauna resonator diaphragms',2,'Faunal membrane','Music','Membrane','Signaling chamber; retain a matched pair of tension membranes',
 'Paired tension membranes from a proposed signaling chamber. Candidate mechanical sounder or instrument diaphragms, distinct from hard resonance plates.',
 'Matched thin membranes are fragile specialist workpieces rather than interchangeable bulk tissue.'),
('whisker_root_fan','Megafauna whisker-root fan',2,'Faunal fiber','Warning and detection','Bundle','Sensory field; preserve a fan of roots and aligned bristles',
 'A fan-shaped root mat retaining the orientation of its sensory bristles. Candidate mechanical contact-feeler research material, not a functioning biological sensor.',
 'The intact orientation pattern has specialist value that loose coarse bristles do not retain.'),
('corneal_windows','Megafauna corneal windows',2,'Faunal hard tissue','Vision','Panel','External eye cover; separate clear faceted protective windows',
 'Clear faceted outer eye-cover panels. Candidate protective viewing windows after inspection; these outer covers are distinct from internal focusing lenses.',
 'Useful clear areas without scoring or cracks are scarce even on a large carcass.'),
('brake_lamellae','Megafauna joint-brake lamellae',2,'Faunal tissue','Machinery drive','Strip','Heavy limb joint; recover opposing friction lamellae',
 'Opposing textured lamellae from a proposed joint-braking structure. Candidate friction-contact research feed rather than a finished clutch or brake.',
 'Paired textured contact layers demand selective extraction and offer a specialist surface unavailable in generic hide.'),
('locking_hooks','Megafauna intersegment locking hooks',2,'Faunal hard tissue','Equipment mounting','Solid part','Segment junction; recover opposed hooks with their roots',
 'Opposed hard hooks from a proposed body-segment latch. Candidate hook-and-catch blanks with broad attachment roots.',
 'Complete opposed geometry and intact roots distinguish these from ordinary broken spines.'),
('spray_nozzle_plate','Megafauna spray-nozzle plate',2,'Faunal hard tissue','Material transfer','Panel','Defensive secretion outlet; retain the perforated outlet plate',
 'A perforated plate surrounding defensive secretion outlets. Candidate patterned dispensing or flow-distribution research material after decontamination.',
 'An intact outlet pattern is difficult to recover from a hazardous gland-bearing hunt.'),
('reservoir_wall','Megafauna secretion-reservoir wall',2,'Faunal membrane','Liquid and bulk containment','Membrane','Defensive reservoir; isolate the thick laminated wall',
 'A thick laminated membrane from a defensive secretion reservoir. Candidate industrial liner feed, distinct from thin gut lining and ordinary joint membrane.',
 'The layered wall is selectively recoverable material from a hazardous internal compartment.'),
('precursor_pellets','Megafauna gland precursor pellets',2,'Biological chemical','Chemical processing','Particles','Secretion gland; collect segregated solid precursor pellets',
 'Solid deposits proposed as glandular secretion precursors. Candidate industrial reaction feed for later analysis, separate from collected final secretion.',
 'Segregated precursor deposits are a specialist sample from difficult-to-access gland anatomy.'),
('jaw_sealing_rings','Megafauna jaw-joint sealing rings',2,'Faunal membrane','Sealing','Solid piece','Jaw pivot; retain the annular flexible sealing margin',
 'Annular flexible margins from a giant jaw pivot. Candidate shaped seal blanks, distinct from flat joint-membrane patches.',
 'A complete flexible ring with a usable central opening is harder to obtain than membrane scraps.'),
('anchoring_talon','Megafauna anchoring talon',2,'Faunal hard tissue','Equipment mounting','Solid piece','Cliff-clinging foot; remove a complete recurved talon and root',
 'A complete recurved anchoring talon with a broad root. Candidate hooked mounting or anchoring stock, distinct from small detached claw tips.',
 'The full root-to-hook profile is a specialist blank recovered from a difficult climbing hunt.'),
('vibration_rods','Megafauna vibration-transfer rods',2,'Faunal hard tissue','Warning and detection','Solid piece','Sensory limb; isolate long rods linking contact surfaces to sensory roots',
 'Long slender rods from a proposed vibration-transfer pathway. Candidate mechanical contact-transmission experiments, separate from short joint hinge pins.',
 'Long unfractured sensory rods are fragile specialist harvests rather than common structural splinters.'),
('brood_seam_comb','Megafauna brood-seam comb',2,'Faunal hard tissue','Fiber processing','Solid piece','Brood-bearing adult; retain the fine-toothed casing-opening appendage',
 'A fine-toothed appendage proposed for opening brood-casing seams. Candidate seam-separating or fiber-combing blank, with different tooth geometry from grooming plates.',
 'Specialized fine teeth and a complete supporting base justify a specialist value band.'),
('reflector_mosaic','Megafauna reflector mosaic',2,'Faunal hard tissue','Decoration','Panel','Display surface; preserve an ordered reflective microplate patch',
 'A patch of ordered reflective microplates from a display surface. Candidate decorative inlay that relies on retained structure rather than extracted pigment.',
 'An uncrushed ordered patch is scarce intact material even when ordinary shell flakes are plentiful.'),
('wing_root_ligament','Megafauna wing-root ligament',2,'Faunal tissue','Equipment fabrication','Bundle','Large flyer; extract the broad elastic wing-root linkage',
 'A broad elastic linkage from a wing root. Candidate flexing connector feed after stabilization, distinct from tensile cordage and delicate sail membrane.',
 'A complete broad linkage is a selective, damage-sensitive harvest from a large flying quarry.'),
('shock_joint_core','Megafauna shock-damping joint core',3,'Faunal tissue','Equipment fabrication','Assembly','Massive weight-bearing joint; preserve the nested cushion and support core',
 'A nested joint core retaining its supporting layers and compliant cushion. Candidate damping-assembly research material rather than loose elastic arches.',
 'The layered interface is rarely recovered intact after a hard fight and is difficult to replace with separated tissue.'),
('ventral_keel','Megafauna laminated ventral keel',3,'Faunal hard tissue','Binding and construction','Solid piece','Armored underside; lift the long laminated internal keel',
 'A long laminated support from beneath an armored body. Candidate structural blank whose value depends on continuity through several attached layers.',
 'Protected internal placement, length and intact lamination make it a high-value selective harvest.'),
('flexor_comb','Megafauna flexor locking comb',3,'Faunal hard tissue','Machinery drive','Assembly','Heavy gripping limb; recover the opposed toothed locking elements',
 'Opposed toothed elements from a proposed limb-holding mechanism. Candidate mechanical locking research stock, not a working ratchet or autonomous actuator.',
 'Matched fine tooth engagement and retained supports are scarce intact anatomical geometry.'),
('lens_cluster','Megafauna precision lens cluster',3,'Faunal tissue','Vision','Assembly','Specialized eye; isolate an aligned cluster without its outer cover',
 'An aligned internal lens cluster retained in its supporting tissue. Candidate optical research feed, distinct from loose individual lenses and a whole ocular organ.',
 'Alignment is easily destroyed during harvest, making intact clusters high-value specialist material.'),
('actuator_braid','Megafauna actuator braid',3,'Faunal tissue','Machinery drive','Bundle','Heavy electroactive limb; preserve an organized braid with attachment ends',
 'An organized braid of proposed electroactive tissue with both attachment ends. Candidate actuator-material research, not ordinary loose fiber or working artificial muscle.',
 'Intact end attachments and organized internal structure create high replacement difficulty.'),
('charge_septa','Megafauna charge-organ septum stack',3,'Faunal membrane','Electrical insulation','Assembly','Charge-bearing animal; retain the ordered internal septa',
 'An ordered stack of partitions from a proposed charge organ. Candidate insulation and compartment-separation research feed, distinct from generic organ tissue.',
 'Ordered undamaged partitions are scarce internal structures from a hazardous hunt.'),
('discharge_lining','Megafauna discharge-channel lining',3,'Faunal tissue','Electrical work','Strip','Charge-bearing animal; isolate the protected discharge-path lining',
 'A continuous lining from a proposed electrical discharge path. Candidate electrical-interface research material; harvest grants no stored charge or working discharge system.',
 'Hazardous extraction and retaining a continuous undamaged lining make this high-value material.'),
('dielectric_baffles','Megafauna dielectric chamber baffles',3,'Faunal hard tissue','Electrical insulation','Panel','Charge-bearing animal; separate the rigid chamber-dividing baffles',
 'Rigid chamber-dividing baffles from a proposed electrical organ surround. Candidate shaped insulation research blanks, distinct from flexible septum membranes.',
 'Uncracked shaped baffles are rare selective recoveries from protected internal anatomy.'),
('acoustic_horn','Megafauna acoustic focusing horn',3,'Faunal hard tissue','Warning and detection','Solid piece','Signaling or sensing chamber; preserve its internal tapered passage',
 'A tapered internal passage from a proposed acoustic organ. Candidate sound-routing research blank, distinct from an external horn core or musical horn.',
 'Continuous internal geometry is difficult to extract without splitting the body of the passage.'),
('scent_ampulla','Megafauna scent-gland ampulla',3,'Biological chemical','Fauna interaction','Assembly','Rare signaling animal; isolate a compartmentalized scent-gland specimen',
 'A compartmentalized scent-gland specimen retaining separated residues. Candidate species-response research, distinct from a mixed scent-marker secretion.',
 'Preserved compartment boundaries and rare source availability justify high sample value.'),
('bonding_matrix','Megafauna bonding-gland matrix',3,'Faunal tissue','Chemical processing','Solid piece','Armored specialist; isolate the structured secretion-producing matrix',
 'Structured tissue proposed to produce a shell-bonding secretion. Candidate investigation of source processing chemistry, separate from collected binder secretion.',
 'Recovering the intact producing structure is substantially harder than collecting discharged material.'),
('catalytic_granules','Megafauna catalytic digestion granules',3,'Biological chemical','Chemical processing','Particles','Rare mineral-processing gut; segregate intact dense granules',
 'Dense granules from a proposed mineral-processing digestive compartment. Candidate catalytic-material analysis feed, not a qualified etchant, enzyme or safe reagent.',
 'Rare source anatomy and difficult separation of intact granules give these high specialist value.'),
('heat_lattice','Megafauna heat-exchange lattice',3,'Biological and mineral mixture','Equipment fabrication','Assembly','Heat-tolerant hunt quarry; recover the internal branching exchange scaffold',
 'A branching scaffold proposed to distribute heat within a giant animal. Candidate thermal-interface research stock with its fine passages preserved.',
 'An intact fine-passage lattice is a fragile and difficult internal recovery rather than bulk shell.'),
('reservoir_sphincter','Megafauna reservoir sphincter',3,'Faunal tissue','Material transfer','Assembly','Defensive reservoir; recover the complete annular closure structure',
 'A complete annular closure from a proposed defensive reservoir. Candidate compliant-valve research material, distinct from reservoir wall membrane.',
 'Recovering the complete closure and attachment interfaces carries high extraction and preservation difficulty.'),
('icore_root_mat','Megafauna long-strand icore root mat',3,'Husk fiber','Textile processing','Bundle','Rare fiber-bearing giant; retain long strands rooted in their support mat',
 'A root mat holding unusually long aligned icore strands. Candidate low-yield specialist fiber stock, distinct from abundant short scraped icore fibers.',
 'Length and continuous root alignment are scarce recoverable qualities; difficult textile processing still applies.'),
('spar_junction','Megafauna wing-spar junction',3,'Faunal hard tissue','Wind propulsion','Assembly','Large flying hunt quarry; extract the intact branched wing-root spar junction',
 'A branched spar junction retaining its multiple attachment surfaces. Candidate sail-frame connection research stock, distinct from straight individual wing ribs.',
 'The unbroken branching junction is a high-value geometry commonly destroyed by a fall or a fight.'),
('intact_charge_capsule','Megafauna intact charge-organ capsule',4,'Faunal tissue','Electrical work','Assembly','Exceptionally rare charge-bearing giant; recover the complete organ envelope and internal supports',
 'A complete charge-organ specimen with its envelope and internal supports retained. An exceptional research harvest, distinct from tissue samples, septa or channel linings. It is not a charged battery.',
 'Exceptionally scarce source, dangerous access and near-total structural preservation make a complete specimen an exceptional prize.'),
('navigation_organ','Megafauna acoustic navigation organ',4,'Faunal tissue','Warning and detection','Assembly','Exceptionally rare sensing specialist; isolate the complete acoustic receiving structure',
 'A complete proposed acoustic receiving organ with its membranes, passages and support frame. Candidate sensory-material research, not a working detector or a granted tracking ability.',
 'A whole delicate sensory structure from an exceptionally rare hunt has far greater scarcity than detached acoustic parts.'),
('ocular_core','Megafauna intact ocular core',4,'Faunal tissue','Vision','Assembly','Exceptionally rare giant with specialized vision; retain the complete supported internal eye core',
 'A complete internal ocular specimen retaining lens supports, separating layers and receptor tissue. Candidate specialist research material, distinct from a detached lens cluster or corneal window.',
 'Complete internal organization is exceptionally difficult to preserve after a dangerous hunt.'),
('load_transfer_hub','Megafauna mineral-thread load hub',4,'Biological and mineral mixture','Binding and construction','Assembly','Exceptionally rare armored giant; recover the internal node where mineralized threads converge',
 'A dense internal node joining several mineralized tension paths. Candidate study of load-spreading biological composites with the converging structure intact.',
 'An intact multi-path node is an exceptional internal recovery, rather than another large sheet of cheap shell.'),
('continuous_fiber_spool','Megafauna continuous fiber coil',4,'Faunal fiber','Textile processing','Bundle','Exceptionally rare fiber-producing giant; unwind and retain one continuous internally coiled strand',
 'One unusually long continuous strand from a proposed internal fiber-storage structure. Candidate specialist cordage or textile feed, distinct from short scraped fibers and woven finished material.',
 'Unbroken length and scarce source anatomy justify exceptional value; difficult processing and low usable yield still apply.'),
('separation_gland','Megafauna chemical-separation gland',4,'Faunal tissue','Chemical processing','Assembly','Exceptionally rare chemical specialist; retain the intact multi-compartment gland',
 'An intact gland proposed to segregate chemically different internal fractions. Candidate study of selective barriers and industrial separation materials, not a ready chemical refinery.',
 'A whole undamaged multi-compartment specimen from an exceptionally rare source is an exceptional specialist harvest.'),
('reflector_organ','Megafauna layered reflector organ',4,'Faunal hard tissue','Decoration','Assembly','Exceptionally rare display specialist; recover the complete ordered reflector stack',
 'A complete stack of ordered reflective layers with its spacing supports intact. Candidate optical or decorative research material, distinct from surface reflector mosaic patches.',
 'Preserving the full layered order is exceptionally difficult and materially different from gathering reflective shell flakes.'),
('pressure_valve_organ','Megafauna pressure-regulation organ',4,'Faunal tissue','Material transfer','Assembly','Exceptionally rare reservoir-bearing giant; isolate the complete regulator and its attachments',
 'A proposed regulator organ with its multiple closure stages retained. Candidate controlled-flow research, distinct from a single reservoir sphincter. No pressure rating or stored driving medium is supplied.',
 'Multiple intact linked stages from an exceptionally rare giant make this an exceptional research recovery.'),
('molt_template','Megafauna molt-growth scaffold',4,'Faunal tissue','Fabrication','Assembly','Exceptionally rare mature giant; preserve the delicate layered growth-interface scaffold',
 'A delicate scaffold from the interface where a proposed giant forms replacement hard covering. Candidate study of layered material formation, not a living production system or a source of regenerative effects.',
 'The complete fragile interface is exceptionally scarce and requires an unusual intact harvest rather than ordinary shed husk collection.'),
('signal_pearl','Megafauna layered signal pearl',4,'Biological and mineral mixture','Specialist samples','Solid piece','Exceptionally rare mature signaling giant; isolate a complete internally layered resonance nodule',
 'A large internally layered nodule proposed to sit within a signaling structure. Candidate acoustic-material sample or collector specimen, distinct from small balance stones. It has no assigned supernatural property.',
 'A complete mature layered specimen combines exceptional source scarcity with difficult damage-free recovery.'),
]


def specification():
    assert len(PROPOSALS) == 50
    assert Counter(row[2] for row in PROPOSALS) == {2: 24, 3: 16, 4: 10}
    defaults = dict(Tier='T2', Category='Biological resources', Subtype='Megafauna hunt material',
        **{'Implementation scope':'New proposal', 'Equipment slot':'None',
           'Carrier metadata':'Physical hunt cargo; compatible packing, containment, mass and handling remain unresolved.',
           'Stack limit':None, 'Mass (game units)':None, 'Trade value':None, 'Damage':None,
           'Protection':None, 'Durability':None, 'Supported behavior':'Not implemented',
           'Crafting / processing':'Select and preserve the anatomical specimen for later qualification. No recipe, usable yield or operating performance established.',
           'Limitations':'Potentially toxic or contaminated biological cargo. No edibility, human filtration, medical use, rated protection, live-organ function or stored usable energy is established.',
           'Item type':'Raw material', 'Processing stage':'Raw', 'Origin family':'Fauna',
           'Design status':'Proposal', 'Crafting role':'Material', 'Finished-item input tier':'Unassigned',
           'Classification basis':'Author-requested great-hunt biological material brainstorm. Specific anatomy and receiving designs remain proposals.'})
    items = []
    for slug,name,rank,material,use,form,anatomy,purpose,basis in PROPOSALS:
        items.append(dict(id='proposal.megafauna.'+slug, name=name, purpose=purpose,
            icon=f'One isolated harvested specimen: {name.removeprefix("Megafauna ")}. Grounded insectoid/faunal structure, no containers, text, wood or assembled equipment.',
            material=material, use=use, fields={
                'Tier':f'T{rank}', 'Rarity':MAPPING[f'T{rank}'], 'Material family':material,
                'Primary use':use, 'Physical form':form,
                'Acquisition':f'Great-hunt harvest proposal. {anatomy}. Specific creature, extraction method, encounter frequency and yield remain design work.',
                'Tier basis':f'T{rank} practical-worth proposal. {basis} This is harvested material value, not a refinement stage or crafting qualification.',
                'Search aliases':f'megafauna; giant fauna; great hunt; rare creature harvest; {slug.replace("_"," ")}',
                'Open design decisions':'Source species and exact anatomy, intact-yield conditions, preservation, compatibility and receiving recipes remain open. Artwork direction: '+name.removeprefix('Megafauna ')+'.'}))
    return dict(date='2026-10-16', source='Direct author direction: extremely rare giant megafauna form difficult great hunts, with higher-value and higher-rarity biological harvests. Specific item anatomy and uses are new local brainstorming proposals.',
        boundary='Catalog-only brainstorming. Ordinary shells remain cheap bulk; no generic meat/fat, general edibility, cloth, wood, natural free water, defined yields, recipes or implemented harvesting introduced. No external lore retrieval or source-canon claims.',
        worldIntegration='World identity, terrain/chunk streaming, unload/reload, generated object identity and persisted runtime deltas do not apply: no Unity world objects or live saves are created or edited. Authored constraints remain in WORLD_BASIS; stable catalog item IDs identify the new proposals.',
        expectedCategoryCounts={'Biological resources':100}, rarityDefaults=MAPPING,
        defaults=defaults, items=items)


def prepare():
    assert not AUDIT.exists(), 'Expansion already applied; preserve later manual rarity changes.'
    OUT.mkdir(parents=True, exist_ok=True)
    original = SOURCE.read_bytes()
    sheets = parse_sheets(original)
    headers = sheets['Items'][4]
    existing = [dict(zip(headers,row), row=number) for number,row in master_items(sheets) if row[3]=='Biological resources']
    assert len(existing)==50, 'Biological roster changed; review the expansion target.'
    spec = specification()
    SPEC.write_bytes((json.dumps(spec, indent=2)+'\n').encode('utf-8'))
    plan = dict(sourceHash=digest(original), spec=spec, existing=existing)
    (OUT/'before.xlsx').write_bytes(original)
    (OUT/'plan.json').write_text(json.dumps(plan, indent=2)+'\n', encoding='utf-8')
    # The stage builder projects these existing source fields before editing.
    print('Prepared 50 retained items and 50 megafauna proposals.')


def build(snapshot, spec):
    appended, receipt = append.build(snapshot, spec)
    sheets = parse_sheets(appended)
    headers = sheets['Items'][4]
    new_ids = {item['id'] for item in spec['items']}
    changes = {'Items':{}}
    audit_edits = {}
    decisions = []
    for number,row in master_items(sheets):
        item = dict(zip(headers,row))
        if item['Category']!='Biological resources':
            continue
        rarity = MAPPING[item['Tier']]
        if item['Rarity']!=rarity:
            changes['Items'][f'P{number}'] = rarity
        decisions.append(dict(id=item['Item ID'], name=item['Item name'], tier=item['Tier'],
                              previousRarity=item['Rarity'], rarity=rarity, added=item['Item ID'] in new_ids))
        if item['Item ID'] in new_ids:
            audit_edits[f'E{number}'] = 'Great-hunt biological harvest'
    assert len(decisions)==100
    candidate = merge_rarities(appended,changes) if changes['Items'] else appended
    buffer = BytesIO()
    with ZipFile(BytesIO(candidate)) as archive, ZipFile(buffer,'w') as output:
        part = parts_for(archive)['Craft audit']
        revised = patch_cells(archive.read(part).decode(),audit_edits).encode()
        for info in archive.infolist():
            output.writestr(info,revised if info.filename==part else archive.read(info.filename))
    result = buffer.getvalue()
    verify_move(candidate,result,{'Craft audit':audit_edits},category_contract=False)
    after = parse_sheets(result)
    before_items = {row[0]:row for _,row in master_items(parse_sheets(snapshot))}
    after_items = {row[0]:row for _,row in master_items(after)}
    assert len(after_items)==len(before_items)+50
    for key,old in before_items.items():
        new = after_items[key]
        if old[3]=='Biological resources':
            assert old[:15]+old[16:]==new[:15]+new[16:], key
        else:
            assert old==new, key
    assert Counter(i['tier'] for i in decisions)=={'T0':33,'T1':17,'T2':24,'T3':16,'T4':10}
    receipt.update(resultHash=digest(result), reviewed=100, added=50, retained=50,
        tierCounts=dict(Counter(i['tier'] for i in decisions)), rarityCounts=dict(Counter(i['rarity'] for i in decisions)),
        existingRarityChanges=changes['Items'], items=decisions)
    return result,receipt


def apply():
    plan = json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    assert not AUDIT.exists()
    assert digest(SOURCE.read_bytes())==plan['sourceHash'], 'Workbook changed; rebuild from the latest snapshot.'
    assert json.loads(SPEC.read_text(encoding='utf-8'))==plan['spec']
    book = openpyxl.load_workbook(OUT/'authored.xlsx',read_only=True,data_only=True)
    rows = list(book['Catalog inputs'].values)[1:]
    assert len(rows)==50
    for row,item in zip(rows,plan['spec']['items']):
        assert list(row[:6])==[item[k] for k in ['id','name','purpose','icon','material','use']]
        assert json.loads(row[6])==item['fields']
    defaults = list(book['Rarity defaults'].values)[1:]
    assert len(defaults)==50
    for row,item in zip(defaults,plan['existing']):
        assert list(row)==[item['Item ID'],item['Tier'],MAPPING[item['Tier']]]
    book.close()
    candidate,receipt = build((OUT/'before.xlsx').read_bytes(),plan['spec'])
    (OUT/'after.xlsx').write_bytes(candidate)
    receipt.update(date='2026-10-16',authority=plan['spec']['source'],
        scope='50 new Biological resources proposals plus editable default Rarity values on this 100-item category only. Existing IDs, tiers, descriptions, other categories, recipes, artwork/review state and Unity/world/save data preserved. No ongoing tier-to-rarity coupling. Google Sheet unsynchronized.',
        validation='Preserving appender and exact changed-cell verification passed. Unrelated OOXML archive members, formulas, styles and existing cells retained; linked use/audit records, category counts, ID pickers and table bounds accounted for.')
    guard.release_guard()
    atomic_write(SOURCE,candidate,plan['sourceHash'])
    atomic_write(AUDIT,(json.dumps(receipt,indent=2)+'\n').encode())
    (OUT/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({key:receipt[key] for key in ['added','retained','reviewed','tierCounts','rarityCounts','resultHash']},indent=2))


if __name__=='__main__':
    guard.OUT=OUT
    guard.prepare=prepare
    guard.apply=apply
    guard.run()
