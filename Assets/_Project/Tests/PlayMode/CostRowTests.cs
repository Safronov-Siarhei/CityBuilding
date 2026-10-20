using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A price reads as a price (the user, 2026-10-20, took the Laboratory's upgrade price for a
    /// second resource count): what the stock cannot cover is red, and turns back the moment it can.
    /// </summary>
    public class CostRowTests
    {
        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTest]
        public IEnumerator WhatTheStockCannotCover_IsRed_UntilItCan()
        {
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Wood, 11);
            resources.SetAmount(ResourceType.Stone, 58);

            var row = new GameObject("Row", typeof(RectTransform)).transform;
            CostRowBuilder.Build(row, new List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 64 },
                new ResourceAmount { type = ResourceType.Stone, amount = 48 },
            }, null);
            var live = row.GetComponent<CostRowLive>();

            Assert.IsTrue(live.IsShort(ResourceType.Wood), "64 wood against a stock of 11 is not marked short.");
            Assert.IsFalse(live.IsShort(ResourceType.Stone), "48 stone against a stock of 58 is marked short.");

            resources.SetAmount(ResourceType.Wood, 100);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(live.IsShort(ResourceType.Wood), "The wood price stayed red after the stock grew past it.");
        }
    }
}
