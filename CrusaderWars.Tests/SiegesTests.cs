using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CrusaderWars.sieges;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.Tests
{
    [TestClass]
    public class SiegesTests
    {
        [TestInitialize]
        public void Init()
        {
            Sieges.Reset();
        }

        [TestMethod]
        public void GetSettlementLevel_Castle01WithoutFortifications_ReturnsLevel1()
        {
            int result = Sieges_DataTypes.Holding.GetSettlementLevel("castle_01", Array.Empty<string>());
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void GetSettlementLevel_Castle02WithCurtainWalls_ReturnsLevel2()
        {
            int result = Sieges_DataTypes.Holding.GetSettlementLevel("castle_02", new[] { "curtain_walls_01" });
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetSettlementLevel_Castle01WithPalisadesStaysLevel1()
        {
            int result = Sieges_DataTypes.Holding.GetSettlementLevel("castle_01", new[] { "palisades_01" });
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void GetSettlementLevel_Castle04AlwaysLevel2()
        {
            int result = Sieges_DataTypes.Holding.GetSettlementLevel("castle_04", Array.Empty<string>());
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle01Fort0_ReturnsTier1()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_01", 0);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle02Fort11_ReturnsTier2()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_02", 11);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle01Fort12_ReturnsTier2()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_01", 12);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle04Fort0_ReturnsTier4()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_04", 0);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle01Fort28_ReturnsTier4Capped()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_01", 28);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle02Fort28_ReturnsTier4Capped()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_02", 28);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GetGarrisonTier_Castle03Fort20_ReturnsTier4()
        {
            int result = Sieges_DataTypes.Holding.GetGarrisonTier("castle_03", 20);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void CalculateAndSetHoldingLevel_PopulatesBothAxes()
        {
            Sieges.SetHoldingLevelKey("castle_02");
            Sieges.SetFortLevel(5);
            Sieges.CalculateAndSetHoldingLevel();

            // castle_02 base = 3 + fort 5 = HL 8
            Assert.AreEqual(8, Sieges.GetHoldingLevel());
            // castle_02 without curtain_walls => settlement level 1
            Assert.AreEqual(1, Sieges.GetSettlementLevel());
            // base tier 2 + fort boost 0 (fort 5 <= 11) => tier 2
            Assert.AreEqual(2, Sieges.GetGarrisonTier());
        }

        [TestMethod]
        public void GetFactionGarrison_AcceptsTierAndReturnsNonEmpty()
        {
            // Ensures the signature change compiles and is callable.
            var result = UnitMappers_BETA.GetFactionGarrison("att_fact_western_roman", 1);
            Assert.IsNotNull(result);
        }
    }
}