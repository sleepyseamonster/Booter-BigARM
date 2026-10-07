#include "Assets/GltfImport.h"
#include "Animation/AnimationPlayer.h"
#include <iostream>
int main(int argc,char** argv) try {
    if(argc!=3)throw std::runtime_error("Usage: engine_model_cook source.glb new-output-directory");
    auto model=std::make_shared<engine::ModelData>(engine::importGltf(argv[1]));
    engine::AnimationPlayer player(model);player.rest();
    for(size_t i=0;i<model->clips.size();++i)player.sample(i,model->clips[i].duration*.25);
    engine::saveModel(argv[2],*model);
    std::cout<<"Cooked "<<model->vertices.size()<<" vertices, "<<model->joints.size()<<" skin joints, "<<model->clips.size()<<" clips\n";return 0;
} catch(const std::exception& error) {std::cerr<<error.what()<<'\n';return 1;}
