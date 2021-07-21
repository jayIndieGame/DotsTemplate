using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Assertions;
using Hash128 = Unity.Entities.Hash128;

public class SubSceneReference : MonoBehaviour
{
    private static SubSceneReference instance;

    public static SubSceneReference Instance
    {
        get
        {
            Assert.IsTrue(FindObjectsOfType<SubSceneReference>().Length == 1);
            return instance;
        }
        private set { }
    }

    public SubScene[] SubSceneArray;

    public void Awake()
    {
        instance = this;
    }
}
