using UnityEngine;
public interface IGrabbable
{
    void Grab();
    void Release(UnityEngine.Vector3 throwVelocity);
    void SetHighlight(bool active, UnityEngine.Color color = default, float width = 3f);
}

public interface IDice
{
    void Roll(UnityEngine.Vector3 force, UnityEngine.Vector3 torque);
    int GetResult();
}

public interface IEffectable
{
    void ApplyModifier(IModifier modifier);
    void RemoveModifier(IModifier modifier);
}

public interface IModifier
{
    string ModifierName { get; }
}