using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Westwind.Data.EfCore;
using Westwind.Utilities;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business.Models
{
    public class UserBusiness : EntityFrameworkBusinessObject<WeblogContext,User>
    {        

        public UserBusiness(WeblogContext context, WeblogConfiguration config) : base(context)
        {
            Context = context;
            Configuration = config;
        }

        /// <summary>
        /// Entity Framework context instance
        /// </summary>
        public WeblogContext Context;

        /// <summary>
        /// Weblog application configuration instance
        /// </summary>
        public WeblogConfiguration Configuration;


        /// <summary>
        /// Holds validation errors when saving or for adding
        /// errors during additional validation steps
        /// </summary>
        //public ValidationErrorCollection Errors { get; set; } = new ValidationErrorCollection();



        /// <summary>
        /// Authenticates a user by username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool AuthenticateUser(string username, string password)
        {
            var user = AuthenticateAndRetrieveUser(username, password);
            if (user == null)
                return false;
            
            return true;
        }


        /// <summary>
        /// Authenticates a user by username and password and returns the
        /// user instance
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public User AuthenticateAndRetrieveUser(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    SetError("Invalid username or password.");
                    return null;
                }

                // assumes only no dupe email addresses
                var user = GetUserByEmail(username);
                if (user == null)
                {
                    SetError("Invalid username or password.");
                    return null;
                }                

                string passwordHash = HashPassword(password, user.Id.ToString());
                if (user.Password != passwordHash && user.Password != password)
                {
                    SetError("Invalid username or password.");
                    return null;
                }            

                return user;
            }
            catch (Exception ex)
            {
                SetError(ex);
                return null;
            }
        }


        /// <summary>
        /// Retrieves an individual User
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public User GetUser(int id)
        {
            return  Context.Users.FirstOrDefault(usr => usr.Id == id);
        }

        /// <summary>
        /// Returns an individual user by user name
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public User GetUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                SetError("Username cannot be blank.");
                return null;
            }
            return Context.Users.FirstOrDefault(usr => usr.Username == username);
        }


        /// <summary>
        /// Returns an individual user by Email address (which is actually the username)
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public User GetUserByEmail(string email)
        {
            return GetUserByUsername(email);
        }

        /// <summary>
        /// Writes the actual user to the database using 
        /// EF model.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public User SaveUser(User user)
        {
            bool isNewUser = false;

            var curUser = Context.Users
                .FirstOrDefault(usr => usr.Username == user.Username);

            if (curUser == null)
            {
                curUser = new User();
                var id = curUser.Id;
                DataUtils.CopyObjectData(user, curUser);
                curUser.Id = id;
                Context.Users.Add(curUser);
                isNewUser = true;
            }
            else
            {
                curUser.Username = user.Username;
                curUser.Fullname = user.Fullname;
                curUser.Password = user.Password;                
            }

            if (!Validate(curUser, isNewUser))
                return null;

            Context.SaveChanges();

            return curUser;
        }


        public bool DeleteUser(int userId)
        {
            var user = Context.Users             
                .FirstOrDefault(usr => usr.Id == userId);

            if (user == null)
                return false;

            //foreach (var role in user.Roles)
            //    user.Roles.Remove(role);
            //Context.Users.Remove(user);

            Context.SaveChanges();

            return true;
        }


        const string HashPostFix = "|~|";

        /// <summary>
        /// Returns an hashed and salted password.
        /// 
        /// Encoded Passwords end in || to indicate that they are 
        /// encoded so that bus objects can validate values.
        /// </summary>
        /// <param name="password">The password to convert</param>
        /// <param name="uniqueSalt">
        /// Unique per instance salt - use user id</param>
        /// <param name="appSalt">Salt to apply to the password</param>
        /// <returns>Hashed password. If password passed is already a hash
        /// the existing hash is returned
        /// </returns>
        public static string HashPassword(string password, string uniqueSalt,
            string appSalt = "#254-31%*36@")
        {

            // don't allow empty password
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            // already encoded
            if (password.EndsWith(HashPostFix))
                return password;

            string saltedPassword = uniqueSalt + password + appSalt;

            // pre-hash
            var sha = new SHA1CryptoServiceProvider();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(saltedPassword));

            // hash again
            var sha2 = new SHA256CryptoServiceProvider();
            hash = sha2.ComputeHash(hash);

            return StringUtils.BinaryToBinHex(hash) + HashPostFix;
        }

        #region Validations

        public bool Validate(User user, bool isNewUser = false)
        {
            ValidationErrors.Clear();

            if (isNewUser)
            {
                if (Context.Users.Any(usr => usr.Username == user.Username))
                    ValidationErrors.Add("Email address is already in use.");
            }

            if (string.IsNullOrEmpty(user.Username))
                ValidationErrors.Add("Email address can't be empty.");

            if (string.IsNullOrEmpty(user.Fullname))
                ValidationErrors.Add("Full name can't be empty.");

            if (string.IsNullOrEmpty(user.Password))
                ValidationErrors.Add("Password can't be empty.");
            else
            {
                // always force password to be updated and hashed even if it was entered as plain text            
                user.Password = HashPassword(user.Password, user.Id.ToString());
            }

            if (ValidationErrors.Count > 0)
            {
                ErrorMessage = ValidationErrors.ToString();
                return false;
            }
            return true;
        }

        #endregion

        #region CRUD Error Handling

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
                ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }

        #endregion



    }
}
