#version 330 core
out vec4 FragColor;

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

// parte spotlight
struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;       
};

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;
uniform sampler2D shadowMap;

uniform vec3 viewPos;
uniform Light dirLight;
uniform SpotLight spotLight;

float shininess = 64.0f;

float ShadowCalculation(vec4 fragPosLightSpace);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main()
{             
    // ambient
    vec4 ambient = vec4(dirLight.ambient, 1.0) * texture(texture_diffuse1, fs_in.TexCoords);
  	
    // diffuse 
    vec3 norm = normalize(fs_in.Normal);
    // vec3 lightDir = normalize(light.direction);
    vec3 lightDir = normalize(dirLight.position - fs_in.FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec4 diffuse = vec4(dirLight.diffuse, 1.0) * diff * texture(texture_diffuse1, fs_in.TexCoords);  
    
    // specular
    vec3 viewDir = normalize(viewPos - fs_in.FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);  
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
    vec4 specular = vec4(dirLight.specular, 1.0) * spec * texture(texture_specular1, fs_in.TexCoords);  

    // calculate shadow
    float shadow = ShadowCalculation(fs_in.FragPosLightSpace);        
    vec4 result = (ambient + (1.0 - shadow) * (diffuse + specular));
    // add spotlight contribution
    result = result + vec4(CalcSpotLight(spotLight, norm, fs_in.FragPos, viewDir), 0.0);
    
    FragColor = result;
    
}

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // check whether current frag pos is in shadow
    // shadow acne fix with bias
    float bias = 0.005;
    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }
    shadow /= 9.0;
    // oversampling
    if(projCoords.z > 1.0)
        shadow = 0.0;

    return shadow;
}

// calculates the color when using a spot light.
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * (distance * distance));    
    // spotlight intensity
    float theta = dot(lightDir, normalize(-light.direction)); 
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    // combine results
    vec3 ambient = light.ambient * vec3(texture(texture_diffuse1, fs_in.TexCoords));
    vec3 diffuse = light.diffuse * diff * vec3(texture(texture_diffuse1, fs_in.TexCoords));
    vec3 specular = light.specular * spec * vec3(texture(texture_specular1, fs_in.TexCoords));
    ambient *= attenuation* intensity;
    diffuse *= attenuation* intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}