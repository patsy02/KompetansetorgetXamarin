﻿using KompetansetorgetXamarin.DAL;
using KompetansetorgetXamarin.Models;
using KompetansetorgetXamarin.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KompetansetorgetXamarin.Controls
{
    class VMStillingerSettings : BaseContentPage
    {
        public ObservableCollection<fagområdeSetting> stillingerSettings { get; set; } //items in GUI
        public ObservableCollection<Location> locationsSettings = new ObservableCollection<Location>(); //items in GUI
        public ObservableCollection<Course> coursesSettings = new ObservableCollection<Course>(); //items in GUI

        private List<string> checkedStudyGroups = new List<string>();
        private List<StudyGroup> studyGroupsFilter = new List<StudyGroup>(); //gets used when retreiving projects/oppgaver in CarouselOppgaver

        public Dictionary<string, string> studyDict { private set; get; }
        public Dictionary<string, string> coursesFilter = new Dictionary<string, string>();

        public static bool cs = false; //changed setting bool to detect when to save settings

        public VMStillingerSettings()
        {
            studyDict = new Dictionary<string, string>();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            GetAllFilters();
            SetSettings();

            foreach (var fagområdeSetting in stillingerSettings)
            {
                fagområdeSetting.OnToggled += ToggleSelection;
            }
        }

        public void ToggleSelection(object sender, EventArgs e)
        {
            var fagområdeSetting = sender as fagområdeSetting;
            System.Diagnostics.Debug.WriteLine("{0} has been toggled to {1}", fagområdeSetting.Name, fagområdeSetting.IsSelected);
            //cs = true;
        }

        public List<string> GetSettings()
        {
            checkedStudyGroups.Clear();
            if (stillingerSettings == null)
            {
                SetSettings();
                return checkedStudyGroups; 
            }
            else
                foreach (fagområdeSetting setting in stillingerSettings)
                {
                    bool checkSwitch = setting.IsSelected;
                    if (checkSwitch == true)
                    {
                        System.Diagnostics.Debug.WriteLine("setting.Name: " + setting.Name);
                        checkedStudyGroups.Add(setting.id);
                    }
                }
            return checkedStudyGroups;
        }

        void SetSettings()
        {

            if (stillingerSettings == null)
            {
                stillingerSettings = new ObservableCollection<fagområdeSetting> { };

                foreach (var sg in studyGroupsFilter)
                {
                    System.Diagnostics.Debug.WriteLine("string name in studyDict.Values: " + sg.name);
                    stillingerSettings.Add(new fagområdeSetting(sg.name, sg.filterChecked, sg.id));
                }
            }
        }

        public void SaveSettings()
        {
            //DbLocation lc = new DbLocation();
            //DbCourse cc = new DbCourse();
            //if (cs == true)
            //{
                DbStudyGroup sgc = new DbStudyGroup();

                foreach (fagområdeSetting setting in stillingerSettings)
                {
                    //gets the name and setting from 
                    string setName = setting.Name;
                    bool setSwitch = setting.IsSelected;

                    foreach (StudyGroup studygroup in studyGroupsFilter)
                    {
                        if (studygroup.name == setName)
                        {
                            studygroup.name = setName;
                            studygroup.filterChecked = setSwitch;
                            break;
                        }
                    }
                }
                sgc.UpdateStudyGroups(studyGroupsFilter);
                //cs = false; //set changedsetting to false after saving
                //CarouselStillinger.pullList = true; //set pullList to true, meaning that any refresh action will reload the list according to new settings
            //}
        }
        void GetAllFilters()
        {
            DbStudyGroup sgc = new DbStudyGroup();
            studyGroupsFilter = sgc.GetAllStudyGroups();
            
            foreach (var studyGroup in studyGroupsFilter)
            {
                studyDict.Add(studyGroup.name, studyGroup.id);
            }
        }


    }
}
