const esc=value=>String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

// Shared second navigation tier. Bumpers belong to the primary row; triggers to this row.
export function subTabs({label,prefix,panel,attribute,items,active}){
  return `<div class="menu-subtabs"><span class="subtab-binding" aria-hidden="true">LT ‹</span><div class="subtab-list" data-subtabs role="tablist" aria-label="${esc(label)}">${items.map(({id,name,hint})=>`<button id="${esc(prefix)}-${esc(id)}" data-subtab ${attribute}="${esc(id)}" role="tab" aria-selected="${active===id}" aria-controls="${esc(panel)}" tabindex="${active===id?0:-1}" class="${active===id?'active':''}"${hint?` title="${esc(hint)}"`:''}>${esc(name)}</button>`).join('')}</div><span class="subtab-binding" aria-hidden="true">› RT</span></div>`;
}
