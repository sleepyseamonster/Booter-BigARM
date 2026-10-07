#pragma once
namespace engine {
enum class SceneDrag { None, Orbit, Pan };
// A gesture belongs to the surface on which it starts. Dragging out of a UI
// control or returning with an already-held button never starts a scene drag.
class SceneCameraControls {
public:
    SceneDrag update(bool left,bool middle,bool right,bool alt,bool space,bool mouseCaptured,bool keyboardCaptured,bool focused) {
        const unsigned buttons=(left?1u:0u)|(middle?2u:0u)|(right?4u:0u);
        const unsigned pressed=buttons&~previous_;previous_=buttons;
        if(!focused){source_=0;mode_=SceneDrag::None;return mode_;}
        if(source_&&(!(buttons&source_)||(modifier_==1&&!alt)||(modifier_==2&&!space))){source_=0;mode_=SceneDrag::None;}
        if(!source_&&!mouseCaptured){
            if(pressed&2){source_=2;modifier_=0;mode_=SceneDrag::Pan;}
            else if((pressed&1)&&space&&!keyboardCaptured){source_=1;modifier_=2;mode_=SceneDrag::Pan;}
            else if((pressed&1)&&alt&&!keyboardCaptured){source_=1;modifier_=1;mode_=SceneDrag::Orbit;}
            else if(pressed&4){source_=4;modifier_=0;mode_=SceneDrag::Orbit;}
        }
        return mode_;
    }
    bool active()const{return source_!=0;}
private:
    unsigned previous_=0,source_=0,modifier_=0;
    SceneDrag mode_=SceneDrag::None;
};
}
