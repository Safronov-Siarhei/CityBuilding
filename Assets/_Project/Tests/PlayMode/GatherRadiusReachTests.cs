using System.Collections.Generic;
using CityBuilder.Maps;
using CityBuilder.Resources;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// What "there is nothing left to gather here" means, which is deliberately NOT what a worker's
    /// own node search means (see CitizenVisualsManager.FindNearestFreeNode).
    ///
    /// This is the question a gatherer's card and its workforce row ask, and the answer sends the
    /// player to pick the building up and move it. So it may only be true when moving is genuinely
    /// the fix: a boulder somebody is already working, or a sapling still growing, are both reasons
    /// to wait rather than reasons to rebuild.
    ///
    /// PlayMode rather than EditMode, and not by preference: ResourceNode registers itself in
    /// OnEnable, and the editor never calls OnEnable on a plain MonoBehaviour. In EditMode every
    /// node this fixture builds is invisible to the registry, so the four tests expecting "nothing
    /// in reach" passed for the wrong reason and the two expecting a node failed.
    /// </summary>
    public class GatherRadiusReachTests
    {
        /// <summary>Far outside any map, so the 600 trees and 140 boulders of whichever scene a previous fixture left loaded cannot answer for the node under test.</summary>
        private static readonly Vector3 EmptyGround = new Vector3(5000f, 0f, 5000f);

        private readonly List<GameObject> _nodes = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var node in _nodes)
            {
                if (node != null) Object.DestroyImmediate(node);
            }
            _nodes.Clear();
        }

        private ResourceNode SpawnNode(ResourceType type, float metresAway)
        {
            var go = new GameObject("TestNode");
            go.transform.position = EmptyGround + new Vector3(metresAway, 0f, 0f);
            var node = go.AddComponent<ResourceNode>();
            node.Initialize(type);
            _nodes.Add(go);
            return node;
        }

        [Test]
        public void ATreeInsideTheRadius_CountsAsSomethingToGather()
        {
            SpawnNode(ResourceType.Wood, 8f);
            Assert.IsTrue(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Wood, 12));
        }

        [Test]
        public void ATreeBeyondTheRadius_DoesNot()
        {
            SpawnNode(ResourceType.Wood, 20f);
            Assert.IsFalse(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Wood, 12),
                "A sawmill would be told the forest is in reach when it is 20 m away.");
        }

        [Test]
        public void TheWrongResource_DoesNotCount()
        {
            SpawnNode(ResourceType.Stone, 2f);
            Assert.IsFalse(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Wood, 12),
                "A boulder was offered to a sawmill.");
        }

        /// <summary>The whole reason the mechanic exists: a worked-out boulder is gone for good, and stone never comes back.</summary>
        [Test]
        public void AWorkedOutBoulder_DoesNotCount()
        {
            SpawnNode(ResourceType.Stone, 2f).SetRemainingYield(0);

            Assert.IsFalse(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Stone, 20),
                "An empty boulder still counted, so a quarry standing on bare ground would say it was fine.");
        }

        /// <summary>
        /// A claimed node is one this building's own crew is working. Counting it as "nothing in
        /// reach" would tell a quarry with one boulder and one worker to move -- the one case where
        /// the building is doing exactly what it should.
        /// </summary>
        [Test]
        public void ABoulderSomebodyIsAlreadyWorking_StillCounts()
        {
            Assert.IsTrue(SpawnNode(ResourceType.Stone, 3f).TryClaim());

            Assert.IsTrue(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Stone, 20));
        }

        [Test]
        public void BareGround_HasNothingToGather()
        {
            Assert.IsFalse(ResourceNode.AnyHarvestableWithin(EmptyGround, ResourceType.Wood, 12));
        }
    }
}
