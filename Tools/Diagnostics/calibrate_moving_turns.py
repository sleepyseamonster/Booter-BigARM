"""Candidate proposals for contact-aligned turn loops; final mixed poses need qualification."""
import json, itertools, argparse
from pathlib import Path
import numpy as np

def propose(directory):
    reports=[]
    for band, speed, distance, omega, seeds, window in [
        ('Walk',1.4,3.22170615,np.deg2rad(36.1714)/1.1833334,[.02,.25,.52,.75],.05),
        ('Jog',3,4.36681175,np.deg2rad(35.7106)/.75,[.06,.27,.56,.77],.033),
        ('Run',4,5.592029,np.deg2rad(35.7106)/.75,[.06,.27,.56,.77],.033),
        ('FastRun',4.9,5.49517441,np.deg2rad(59.5215),[.1,.35,.6,.85],.025),
        ('Sprint',6.15,5.483365,np.deg2rad(59.5215),[.1,.35,.6,.85],.025)]:
        for side in ('Left','Right'):
            name='MovingTurn'+band+side
            d=json.loads((directory/(name+'.json')).read_text(encoding='utf-8-sig'))
            t=np.array([f['seconds'] for f in d['frames']]);v=np.array([f['vertices'] for f in d['frames']]);ids=np.array([x['side'] for x in d['ids']]);period=t[-1];floor=d['floor'];minimum=v[:,:,1].min(1)
            w=omega*(1 if side=='Right' else -1);count=max(2,round(window/np.median(np.diff(t))))
            candidates=[]
            for plant,seed in enumerate(seeds):
                foot=1 if plant%2==0 else 0; selected=[]
                # Mirroring shifts the first right contact to the former left contact.
                actual_seed=seed
                if band in ('Jog','Run') and side=='Left':actual_seed=[.02,.27,.52,.77][plant]
                for first in range(len(t)-count):
                    stop=first+count;center=(t[first]+t[stop])/(2*period)
                    if abs(center-actual_seed)>.065:continue
                    part=v[first:stop+1];low=part[:,ids==foot,1].min(1)
                    if (floor-minimum[first:stop+1]).min()<-.03 or (floor-minimum[first:stop+1]).max()>.08:continue
                    if np.max(low-minimum[first:stop+1])>.002:continue
                    dt=t[first:stop+1]-t[first];ct=dt-dt.mean()
                    for vertex in np.flatnonzero(ids==foot):
                        if np.max(part[:,vertex,1]-low)>.008:continue
                        p=part[:,vertex][:,[0,2]]
                        rear=-(np.dot(ct,p[:,1]-p[:,1].mean())/np.dot(ct,ct))+w*p[:,0].mean()
                        if not .5<rear<10:continue
                        theta=w*dt;co=np.cos(theta);si=np.sin(theta)
                        x=co*p[:,0]+si*p[:,1]+rear*(1-co)/w
                        z=-si*p[:,0]+co*p[:,1]+rear*si/w
                        world=np.column_stack((x,z));exc=np.linalg.norm(world-world[0],axis=1).max()
                        if exc>.02:continue
                        selected.append((exc+abs(center-actual_seed)*.15+abs(rear*period*speed/distance-speed)*window*.8,dict(centerPhase=float(center),vertex=int(vertex),side=foot,start=float(t[first]),end=float(t[stop]),nativeFitSpeed=float(rear),curvedExcursion=float(exc),groundGap=float(np.max(part[:,vertex,1]-low)))))
                if not selected:raise ValueError(f'{name} has no measured patch for plant {plant}')
                candidates.append(sorted(selected,key=lambda x:x[0])[:25])
            average_rate=period*speed/distance
            grid=np.array(np.meshgrid(*[np.arange(len(c))for c in candidates],indexing='ij')).reshape(4,-1).T
            contact_array=np.column_stack([np.array([x[1]['centerPhase']for x in candidates[i]])[grid[:,i]]for i in range(4)])
            rates_array=np.diff(np.column_stack((contact_array,contact_array[:,0]+1)),axis=1)*4*average_rate
            valid=(np.diff(contact_array,axis=1)>0).all(1)&(rates_array.min(1)>=.65)&(rates_array.max(1)<=1.35)
            native_speed=np.column_stack([np.array([x[1]["nativeFitSpeed"]for x in candidates[i]])[grid[:,i]]for i in range(4)])
            contact_rates=(rates_array+np.roll(rates_array,1,axis=1))*.5
            scores=sum(np.array([x[0]for x in candidates[i]])[grid[:,i]]for i in range(4))+rates_array.std(1)*.025+np.abs(native_speed*contact_rates-speed).sum(1)*window*.8
            scores[~valid]=np.inf
            if not valid.any():raise ValueError(name+' cannot retain contact and pure source-rate budgets')
            chosen=int(scores.argmin());best=(float(scores[chosen]),[candidates[i][grid[chosen,i]]for i in range(4)],rates_array[chosen])
            patches=[p[1] for p in best[1]];contacts=[p['centerPhase'] for p in patches]
            correction=np.maximum(0,floor-minimum)
            for patch in patches:
                for i,at in enumerate(t):
                    at+=round(((patch['start']+patch['end'])/2-at)/period)*period
                    fade=min((at-patch['start']+.025)/.025,(patch['end']+.025-at)/.025,1)
                    if fade>0 and minimum[i]>floor:correction[i]=min(correction[i],(floor-minimum[i])*fade)
            if correction.max()>.08 or correction.min()<-.03:raise ValueError(name+' exceeds visual height budget')
            reports.append(dict(name=name,band=band,side=side,nominalSpeed=speed,metresPerCycle=distance,duration=float(period),nativeContacts=contacts,sourceTimeMap=[[i*.25,c]for i,c in enumerate(contacts+[contacts[0]+1])],patches=patches,pureRates=best[2].tolist(),nativeAngularSpeed=float(w),sourceCurvatureDegreesPerMetre=float(abs(np.rad2deg(w))/np.median([p['nativeFitSpeed'] for p in patches])),correction=[[float(at/period),float(h)]for at,h in zip(t,correction)]))
            print(name,contacts,'rate',best[2].min(),best[2].max(),'patch',max(p['curvedExcursion']for p in patches))
    return reports

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('directory',type=Path);parser.add_argument('output',type=Path);args=parser.parse_args()
    if args.output.exists():raise FileExistsError('Preserve proposals')
    args.output.write_bytes((json.dumps(propose(args.directory),indent=2)+'\n').encode())
