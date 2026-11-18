using CityBuilder.Saving;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class SaveSystemSanitizeNameTests
    {
        [Test]
        public void KeepsLettersAndDigits()
        {
            Assert.AreEqual("MySave123", SaveSystem.SanitizeName("MySave123"));
        }

        [Test]
        public void ReplacesSpacesWithUnderscores()
        {
            Assert.AreEqual("My_Save", SaveSystem.SanitizeName("My Save"));
        }

        [Test]
        public void StripsPathTraversalCharacters()
        {
            // No dots, slashes, backslashes or colons must survive -- this is what keeps a save
            // name safe to use directly as a file name (see SaveSystem.GetPath).
            Assert.AreEqual("etcpasswd", SaveSystem.SanitizeName("../../etc/passwd"));
            Assert.AreEqual("Ctemp", SaveSystem.SanitizeName("C:\\temp"));
        }

        [Test]
        public void TrimsLeadingAndTrailingUnderscores()
        {
            Assert.AreEqual("Save", SaveSystem.SanitizeName("  Save  "));
        }

        [Test]
        public void EmptyOrWhitespaceInput_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, SaveSystem.SanitizeName(string.Empty));
            Assert.AreEqual(string.Empty, SaveSystem.SanitizeName("   "));
        }

        [Test]
        public void TruncatesToMaxLength()
        {
            var longName = new string('a', 100);
            var result = SaveSystem.SanitizeName(longName);
            Assert.AreEqual(40, result.Length);
        }

        [Test]
        public void KeepsCyrillic_ARussianNameIsAName()
        {
            // the user, 2026-11-18: a Russian name came out empty and the save was never written
            Assert.AreEqual("Моё_сохранение", SaveSystem.SanitizeName("Моё сохранение!"));
        }
    }
}
