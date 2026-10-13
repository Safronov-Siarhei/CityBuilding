using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class EventFeedModelTests
    {
        // Indexed by EventSeverity: LogOnly, Good, Warning, Danger.
        private static readonly float[] Lifetimes = { 0f, 5f, 8f, 12f };

        private static GameEvent Event(EventSeverity severity, EventGlyph glyph = EventGlyph.Fire, string picture = "Baker")
        {
            return GameEvent.Nowhere("text", severity, glyph, picture);
        }

        [Test]
        public void LogOnlyEvents_GetNoIcon()
        {
            var model = new EventFeedModel(4, Lifetimes);

            var slot = model.Add(Event(EventSeverity.LogOnly), out _);

            Assert.IsNull(slot);
            Assert.AreEqual(0, model.Slots.Count);
        }

        [Test]
        public void TheSameIconTwice_CountsUpInsteadOfAddingASecond()
        {
            var model = new EventFeedModel(4, Lifetimes);
            model.Add(Event(EventSeverity.Warning, EventGlyph.Ruin, "Fence"), out _);
            model.Add(Event(EventSeverity.Good, EventGlyph.Star, "Laboratory"), out _);

            var slot = model.Add(Event(EventSeverity.Warning, EventGlyph.Ruin, "Fence"), out _);

            Assert.AreEqual(2, model.Slots.Count);
            Assert.AreEqual(2, slot.Count);
            Assert.AreSame(slot, model.Slots[0], "The icon that counted up is the newest and goes to the top.");
        }

        [Test]
        public void ADifferentPicture_IsADifferentIcon()
        {
            var model = new EventFeedModel(4, Lifetimes);
            model.Add(Event(EventSeverity.Danger, EventGlyph.Fire, "Baker"), out _);
            model.Add(Event(EventSeverity.Danger, EventGlyph.Fire, "Windmill"), out _);

            Assert.AreEqual(2, model.Slots.Count);
        }

        [Test]
        public void CountingUp_StartsTheIconsTimeOver_AndTakesTheNewestPlace()
        {
            var model = new EventFeedModel(4, Lifetimes);
            var first = GameEvent.At("a", EventSeverity.Danger, EventGlyph.Fire, "Baker", new Vector3(1f, 0f, 1f));
            var second = GameEvent.At("b", EventSeverity.Danger, EventGlyph.Fire, "Baker", new Vector3(9f, 0f, 9f));

            model.Add(first, out _);
            model.Tick(10f, null);
            var slot = model.Add(second, out _);

            Assert.AreEqual(12f, slot.Remaining, 1e-4f);
            Assert.AreSame(second, slot.Event, "A tap must go to the latest fire, not the first.");
        }

        [Test]
        public void IconsExpire_AfterTheirSeveritysLifetime()
        {
            var model = new EventFeedModel(4, Lifetimes);
            model.Add(Event(EventSeverity.Good, EventGlyph.Star, "Laboratory"), out _);
            model.Add(Event(EventSeverity.Danger), out _);
            var expired = new List<EventFeedSlot>();

            model.Tick(6f, expired);

            Assert.AreEqual(1, expired.Count, "Good lasts 5 s, danger 12 s.");
            Assert.AreEqual(EventSeverity.Good, expired[0].Event.Severity);
            Assert.AreEqual(1, model.Slots.Count);
            Assert.AreEqual(EventSeverity.Danger, model.Slots[0].Event.Severity);
        }

        [Test]
        public void AFullFeed_DropsTheQuietestOldestIcon_NotADanger()
        {
            var model = new EventFeedModel(3, Lifetimes);
            model.Add(Event(EventSeverity.Danger, EventGlyph.Fire, "A"), out _);
            model.Add(Event(EventSeverity.Good, EventGlyph.Star, "B"), out _);
            model.Add(Event(EventSeverity.Good, EventGlyph.Star, "C"), out _);

            model.Slots[1].View = 7;   // as the controller would have drawn B

            model.Add(Event(EventSeverity.Warning, EventGlyph.Alert, "D"), out var evictedView);

            Assert.AreEqual(7, evictedView, "The view of the oldest good-news icon is the one to hide.");
            Assert.IsFalse(HasPicture(model, "B"), "The oldest good-news icon should have gone.");
            Assert.IsTrue(HasPicture(model, "C"));
            Assert.AreEqual(3, model.Slots.Count);
            Assert.IsTrue(HasPicture(model, "A"), "The danger icon must survive.");
        }

        [Test]
        public void Remove_FreesTheSlotForTheNextEvent()
        {
            var model = new EventFeedModel(1, Lifetimes);
            var slot = model.Add(Event(EventSeverity.Danger), out _);

            model.Remove(slot);
            model.Add(Event(EventSeverity.Good, EventGlyph.Star, "X"), out var evictedView);

            Assert.AreEqual(-1, evictedView);
            Assert.AreEqual(1, model.Slots.Count);
        }

        [Test]
        public void ATarget_FollowsItsTransform_AndFallsBackToWhereItWasOnceGone()
        {
            var go = new GameObject("FollowedThing");
            go.transform.position = new Vector3(3f, 0f, 4f);
            var gameEvent = GameEvent.At("t", EventSeverity.Danger, EventGlyph.Swords, null, new Vector3(-5f, 0f, -5f), go.transform);

            go.transform.position = new Vector3(7f, 0f, 8f);
            Assert.IsTrue(gameEvent.TryGetTarget(out var live));
            Assert.AreEqual(new Vector3(7f, 0f, 8f), live, "While it exists, the target is where it is NOW.");

            Object.DestroyImmediate(go);
            Assert.IsTrue(gameEvent.TryGetTarget(out var fallback));
            Assert.AreEqual(new Vector3(-5f, 0f, -5f), fallback);
        }

        [Test]
        public void AnEventWithNoPlace_HasNoTarget()
        {
            Assert.IsFalse(GameEvent.Nowhere("t", EventSeverity.Good, EventGlyph.Star, null).TryGetTarget(out _));
        }

        private static bool HasPicture(EventFeedModel model, string picture)
        {
            foreach (var slot in model.Slots)
            {
                if (slot.Event.Picture == picture) return true;
            }
            return false;
        }
    }
}
