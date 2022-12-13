#ifndef NAGULE_OBJECT
#define NAGULE_OBJECT

layout(std140) uniform Object {
    mat4 ObjectToWorld;
    bool IsVariant;
};

#endif