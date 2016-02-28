﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace KompetansetorgetXamarin.Views
{
    public partial class LoginPage : ContentPage
    {
        private App app;
        public LoginPage(App app)
        {
            this.app = app;
            InitializeComponent();
        }


        public void LoginClicked(object sender, EventArgs e)
        {
            string username = "nadia";
            string password = "1234";


            if (username.Equals(usernameEntry.Text) && password.Equals(passwordEntry.Text))
            {
                Resultat.Text = "Velkommen";


            }
            else {
                Resultat.Text = "Feil, prøv igjen";
            }
        }


        private void GlemtPassord(object sender, EventArgs e)
        {

        }

        private void OnBypassLogin(object sender, EventArgs e)
        {
            Navigation.InsertPageBefore(new MainPage(), Navigation.NavigationStack.First());
            Navigation.PopToRootAsync();
        }

        private void OnLoginClicked(object sender, EventArgs e)
        {
            // vi blir stuck inni den pagen så lenge vi ikke har server
            //Navigation.PushModalAsync(new AuthenticationPage());
            new Authenticater(app);

        }

    }
}