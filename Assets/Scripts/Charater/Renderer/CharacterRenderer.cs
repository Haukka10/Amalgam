using System;
using UnityEngine;

public class CharacterRenderer : MonoBehaviour
{
    public static readonly string[] staticDirections =
    {
        "Static N", "Static NW", "Static W", "Static SW",
        "Static S", "Static SE", "Static E", "Static NE"
    };

    public static readonly string[] runDirections =
    {
        "Run N", "Run NW", "Run W", "Run SW",
        "Run S", "Run SE", "Run E", "Run NE"
    };

    private Animator _animator;
    private int _lastDir;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetDirection(Vector2 dir)
    {
        string[] dirArray = null;

        if (dir.magnitude < .01f)
        {
            dirArray = staticDirections;
        }else
        {
            dirArray = runDirections;
            _lastDir = DirectionToIndex(dir, staticDirections.Length);
        }

        _animator.Play(dirArray[_lastDir]);
    }

    private int DirectionToIndex(Vector2 dir, int v)
    {
        Vector2 normDir = dir.normalized;
        float step = 360f / v;
        float halfStep = step / 2;
        float angle = Vector2.SignedAngle(Vector2.up, normDir);
        angle += halfStep;

        if (angle < 0)
            angle += 360;

        float stepCount = angle / step;

        return Mathf.FloorToInt(stepCount);
    }
}
