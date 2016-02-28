﻿using System;
using System.Collections.Generic;
using System.Linq;
using UAuth;
using Xamarin.Forms;


namespace KompetansetorgetXamarin.Views
{
    public partial class AuthenticationPage : ContentPage
    {

        public AuthenticationPage()
        {
            InitializeComponent();
            IAuth auth = Auth.auth;
            Authenticate(auth.auth2, AccountStore.Create().FindAccountsForService(AuthProvider.Name));
        }

        void Authenticate(IOAuth2Authenticator auth, List<Account> accounts)
        {
            if (accounts.Count == 0)
            {
                auth.Completed += (sender, eventArgs) =>
                {
                    if (eventArgs.IsAuthenticated)
                    {
                        try
                        {
                            AccountStore.Create().Save(eventArgs.Account, AuthProvider.Name);
                            PerformAuth2TestRequests(eventArgs.Account);
                        }
                        catch (Exception ex)
                        {
                            ErrorLabel.Text = ex.Message;
                        }
                    }
                    else
                    {
                        ErrorLabel.Text = "Authenticate: Not Authenticated";
                        return;
                    }
                };
                auth.Error += (sender, eventArgs) =>
                {
                    ErrorLabel.Text += "Authenticate: Error:" + eventArgs.Message + "\n";
                    Exception ex = eventArgs.Exception;
                    for (Exception inner = eventArgs.Exception; inner != null; inner = inner.InnerException)
                    {
                        ErrorLabel.Text += "Message:" + inner.Message + "\n";
                    }
                    return;
                };
                try
                {
                    // Uten en fungerende server blir vi stuck her i mellomtiden.
                    auth.OAuth2Authenticator(AuthProvider.ClientId, AuthProvider.Scope,
                        new Uri(AuthProvider.AuthorizeUrl), new Uri(AuthProvider.RedirectUrl));
                    System.Diagnostics.Debug.WriteLine("IT RUNS IT RUNS");
                    System.Diagnostics.Debug.WriteLine("IT RUNS IT RUNS");
                    System.Diagnostics.Debug.WriteLine("IT RUNS IT RUNS");
                    System.Diagnostics.Debug.WriteLine("IT RUNS IT RUNS");

                    System.Diagnostics.Debug.WriteLine("IT RUNS IT RUNS");

                    // Navigation.InsertPageBefore(new MainPage(), Navigation.NavigationStack.First());
                    //  Navigation.PopToRootAsync();
                    // Ingenting under her funker som en mellomtids løsning:
                    //Navigation.PopModalAsync();
                    //Navigation.InsertPageBefore(new MainPage(), Navigation.NavigationStack.First());
                    //Navigation.PushModalAsync(new MainPage());

                    //Navigation.PopToRootAsync();


                }
                catch (Exception ex)
                {
                    ErrorLabel.Text = "Authenticate: Exception:";
                    for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    {
                        ErrorLabel.Text += "Message:" + inner.Message + "\n";
                    }
                    // Denne vil kaste en ny exception
                    foreach (KeyValuePair<string, string> p in accounts[0].Properties)
                        ErrorLabel.Text += "Key:" + p.Key + " Value:" + p.Value + "\n";
                }
            }
            else
                PerformAuth2TestRequests(accounts[0]);
                    // TODO: implement error handling. If error is caused by expired token, renew token.
        }

        async void PerformAuth2TestRequests(Account account)
        {
            try
            {
                //await Navigation.PushModalAsync(new ViktorTestView());
                
                ResponseLabel.Text = ""; // clear response display string
                foreach (KeyValuePair<string, string> p in account.Properties)
                {
                    System.Diagnostics.Debug.WriteLine("Property: Key:" + p.Key + " Value:" + p.Value);
                }
                System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: Url:" + AuthProvider.ApiRequests);
                ResponseLabel.Text += "Request Url:" + AuthProvider.ApiRequests + "\n";
                OAuth2Request request = new OAuth2Request("GET", new Uri(AuthProvider.ApiRequests), null, account);
                IResponse response = await request.GetResponseAsync();
                System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: StatusCode:" + response.StatusCode +
                                                   " ResponseUri:" + response.ResponseUri);
                System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: Headers:");
                foreach (KeyValuePair<string, string> h in response.Headers)
                {
                    System.Diagnostics.Debug.WriteLine("Header: Key:" + h.Key + " Value:" + h.Value);
                }
                ResponseLabel.Text += "Response(" + response.StatusCode + "):";
                string r = response.GetResponseText();
                ResponseLabel.Text += r + "\n";
               
                //await Navigation.PushModalAsync(new ViktorTestView());
            }
            catch (Exception ex)
            {
                ErrorLabel.Text += "Exception: PerformAuth2TestRequests: Message:" + ex.Message + "\n";
                foreach (KeyValuePair<string, string> p in account.Properties)
                {
                    ErrorLabel.Text += "Key:" + p.Key + " Value:" + p.Value + "\n";
                }
            }
        }
    }
}