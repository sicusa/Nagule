#ifndef AECO_OBJECT
#define AECO_OBJECT

layout(std140) uniform Object {
    mat4 ObjectToWorld;
    bool IsVariant;
};

#endif