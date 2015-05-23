using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwipCardSystem.Medol
{

    public class ConfigInfo
    {
        public string InstitutionID
        {
            get;
            set;
        }

        public string Today
        {
            get;
            set;
        }

        public string InstitutionName
        {
            get;
            set;
        }

        public string StudentSumNumber
        {
            set;
            get;
        }

        public string ServiceUrl
        {
            get;
            set;
        }

        public Account AdminAccount
        {
            get;
            set;
        }

        public Account StandardAccount
        {
            get;
            set;
        }

        public MyTime BeginTime
        {
            set;
            get;
        }

        public MyTime EndTime
        {
            set;
            get;
        }

        //day
        public int ClearRecordFrequencyByDay
        {
            set;
            get;
        }

        public bool IsFirstUpdate
        {
            set;
            get;
        }

        //s
        public int WaitTimeForToPosterBySecond
        {
            set;
            get;
        }

        public int LastClearRecordDay
        {
            set;
            get;
        }

        public string DateBaseName
        {
            set;
            get;
        }

        public string KaoqinTableName
        {
            set;
            get;
        }

        public string CardTableName
        {
            set;
            get;
        }

        public string FamilyTableName
        {
            set;
            get;
        }

        public string InstitutionTableName
        {
            set;
            get;
        }

        public string StudentTableName
        {
            set;
            get;
        }

        public string CardTableUpdateTime
        {
            set;
            get;
        }

        public string FamilyTableUpdateTime
        {
            set;
            get;
        }

        public string InstitutionTableUpdateTime
        {
            set;
            get;
        }

        public string StudentTableUpdateTime
        {
            set;
            get;
        }

        public string ConnectionString
        {
            set;
            get;
        }

        public string Notice
        {
            set;
            get;
        }
        //update info
        public string ZiXun
        {
            set;
            get;
        }
    }
    public class Account
    {
        public string Name
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }
    }

    public class MyTime
    {
        public int Hour
        {
            get;
            set;
        }
        public int Minute
        {
            get;
            set;
        }
    }
}
