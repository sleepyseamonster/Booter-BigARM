#pragma once
#include "Rendering/TextureStore.h"
#include "Persistence/Document.h"
namespace engine {
Json verifyTextureStore(TextureStore& store,const TextureRecord& record);
}
