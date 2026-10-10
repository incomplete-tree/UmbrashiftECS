using UmbrashiftECS.Components.EntityComponents.Basic;

namespace UmbrashiftECS.Components.EntityComponents;

public record struct MoveBetweenPoints(float Speed, params Position[] Points)
{
    // STATE
    public int NextPointIndex;
}