using System;
using System.Threading.Tasks;
using Westwind.Utilities.Data;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business.Utilities
{
    public class RequestLogger
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="referrer"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static async Task<DbResult<bool>> LogRequest(string postId, string referrer = null, string ipAddress = null)
        {
            var sql = """
                INSERT INTO PostHits
                (
                    PostId,
                    IpAddress,
                    Referrer
                )
                SELECT
                    @PostId,
                    @IpAddress,
                    @Referrer
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM PostHits
                    WHERE PostId = @PostId
                      AND IpAddress = @IpAddress
                      AND [TimeStamp] >= DATEADD(MINUTE, -10, GETDATE())
                )
                """;

            using var db = new SqlDataAccess(wlApp.Configuration.ConnectionString);
            int result = await db.ExecuteNonQueryAsync(sql,
                db.CreateParameter("@PostId", postId),
                db.CreateParameter("@Referrer", referrer),
                db.CreateParameter("@IpAddress", ipAddress));
                
            if (result == -1)
            {
                // hard error
                return new DbResult<bool>
                {
                    Result = false,
                    Message = db.ErrorMessage
                };
            }
            if (result == 0)
            {
                return new DbResult<bool>
                {
                    Result = true,
                    Message = "Previously logged request."
                };
            }

            // added a record - loggable also count as hit
            // in the db
            if (result == 1)
            {
                await db.ExecuteNonQueryAsync("""
                    update Posts set Hits = Hits + 1 where Id = @PostId;
                    """, db.CreateParameter("@PostId", postId));
            }


            return new DbResult<bool>
            {
                Result = result > -1,
                Message = result > -1 ? null : "Failed to log request."
            };
        }


        /// <summary>
        /// Checks to see if the tables exist and if not creates them.
        /// 
        /// Use this method during startup to ensure the tables are present
        /// in the database. Creates `PostHits` table.
        /// </summary>
        /// <param name="forceCreate">If true, creates the tables even if they already exist by dropping first</param>
        /// <returns></returns>
        public static DbResult<bool> EnsureTablesExist(bool forceCreate = false)
        {

            // check to see if the table exists first 
            string sql;
            int result = 0;

            using var db = new SqlDataAccess(wlApp.Configuration.ConnectionString);

            if (!forceCreate)
            {
                // check if table already exists first
                sql = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PostHits'";             
                var res = db.ExecuteScalar(sql);
                if (res != null)
                {
                    return new DbResult<bool>
                    {
                        Result = true,
                        Message = "PostHits table already exists."
                    };
                }
            }
            else
            {
                // force drop table and recreate
                db.ExecuteNonQuery("drop table PostHits");
            }

            sql = """
                create table PostHits (
                  PostId varchar(50),
                  TimeStamp datetime default (getdate()),
                  IpAddress varchar(50) default (''),
                  Referrer nvarchar(512),
                )
                """;

            result = db.ExecuteNonQuery(sql);
            if (result == -1)
            {
                return new DbResult<bool>
                {
                    Result = false,
                    Message = db.ErrorMessage                 
                };                
            }
            return new DbResult<bool>
            {
                Result = result > -1,
                AffectedRows = result
            };
        }


        /// <summary>
        /// Clears out the PostHits table or records older than a specified number of days. 
        /// By default, it clears records older than 7 days. Use 0 to clear the entire table.
        /// 
        /// 
        /// </summary>
        /// <param name="days"></param>
        public static DbResult<bool> ClearRequests(int days = 7)
        {
            var sql = """
                delete from PostHits where TimeStamp < DATEADD(DAY, -@Days, GETDATE())
                """;
            using var db = new SqlDataAccess(wlApp.Configuration.ConnectionString);
            var res = db.ExecuteNonQuery(sql, db.CreateParameter("@Days", days));
            if (res == -1)
            {
                return new DbResult<bool>
                {
                    Result = false,
                    Message = db.ErrorMessage
                };
            }

            return new DbResult<bool>
            {
                Result = true,
                AffectedRows = res
            };
        }



        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
            {
                ErrorMessage = string.Empty;
            }
            else
            {
                Exception e = ex;
                if (checkInner)
                    e = e.GetBaseException();

                ErrorMessage = e.Message;
            }
        }

    }

    public class DbResult<T>
    {
        public T Result { get; set; }

        public int AffectedRows { get; set; }

        public string Message { get; set; }

        public object Data { get; set; }

        /// <summary>
        /// Allow for comparison operators
        /// </summary>
        /// <param name="result"></param>
        public static implicit operator T(DbResult<T> result)
        {
            return result.Result;
        }
    }
}
