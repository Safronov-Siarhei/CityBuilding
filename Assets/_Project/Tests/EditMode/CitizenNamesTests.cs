using System.Collections.Generic;
using CityBuilder.Citizens;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>Every citizen goes by a name of his own (the user, 2026-10-20), not "Житель 3".</summary>
    public class CitizenNamesTests
    {
        [Test]
        public void TheWholeListComesOutBeforeANameRepeats()
        {
            var seen = new HashSet<string>();
            for (var i = 0; i < CitizenNames.Count; i++)
            {
                var name = CitizenNames.Of(i);
                Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"Citizen {i} has no name.");
                Assert.IsTrue(seen.Add(name), $"\"{name}\" is handed out twice in the first {CitizenNames.Count}.");
            }
        }

        [Test]
        public void PastTheListANameComesBackWithANumeral()
        {
            Assert.AreEqual(CitizenNames.Of(0) + " II", CitizenNames.Of(CitizenNames.Count));
            Assert.AreEqual(CitizenNames.Of(5) + " III", CitizenNames.Of(CitizenNames.Count * 2 + 5));
        }
    }
}
