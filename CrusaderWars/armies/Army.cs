        public int GetTotalSoldiers()
        {
            if (ArmyRegiments == null) return 0;
            return ArmyRegiments.Sum(x => x.CurrentNum);
        }

        public int GetTotalLosses()
        {
            if (ArmyRegiments == null) return 0;
            return ArmyRegiments.Sum(ar => ar.GetLosses());
        }

        public int GetTotalSoldiersAfterBattle()
        {
            if (ArmyRegiments == null) return 0;
            return ArmyRegiments.Sum(ar => ar.CurrentNum);
        }
