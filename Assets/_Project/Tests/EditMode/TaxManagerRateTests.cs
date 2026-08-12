using CityBuilder.Resources;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class TaxManagerRateTests
    {
        private GameObject _go;
        private TaxManager _tax;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestTaxManager");
            _tax = _go.AddComponent<TaxManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void DefaultRate_Is10Percent()
        {
            Assert.AreEqual(10, _tax.TaxRatePercent);
        }

        [Test]
        public void SetTaxRate_WithinRange_SetsExactly()
        {
            _tax.SetTaxRate(50);
            Assert.AreEqual(50, _tax.TaxRatePercent);
        }

        [Test]
        public void SetTaxRate_Negative_ClampsToZero()
        {
            _tax.SetTaxRate(-20);
            Assert.AreEqual(0, _tax.TaxRatePercent);
        }

        [Test]
        public void SetTaxRate_Above100_ClampsTo100()
        {
            _tax.SetTaxRate(250);
            Assert.AreEqual(100, _tax.TaxRatePercent);
        }

        [Test]
        public void SetTaxRate_FiresOnTaxRateChanged()
        {
            var fired = false;
            _tax.OnTaxRateChanged += () => fired = true;
            _tax.SetTaxRate(30);
            Assert.IsTrue(fired);
        }
    }
}
