﻿using System;
using System.Collections;
using System.Collections.Generic;

using System.Net.Http;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using KompetansetorgetXamarin.Controls;
using KompetansetorgetXamarin.DAL;
using KompetansetorgetXamarin.Models;
using KompetansetorgetXamarin.Utility;
using Newtonsoft.Json;
using PCLCrypto;
using SQLite.Net;
using SQLiteNetExtensions.Extensions;

namespace KompetansetorgetXamarin.Controllers
{
    public class JobsController : BaseController
    {
        public JobsController()
        {
            Adress += "/jobs";
        }

        /// <summary>
        /// Can return 3 different values:
        /// 1. "exists": No new jobs on the server. 
        /// 2. "incorrectCache": If the cache indicates that local database got data that the server doesnt have. 
        /// 3. "newData": There are new jobs available on the server.
        /// 4. null: Check if Authenticater.Authorized has been set to false, if not the app could most likely not reach the server.
        /// TODO Way to complicated logic, simplify this method if theres time
        /// </summary>
        /// <returns></returns>
        private async Task<string> CheckServerForNewData(List<string> studyGroups = null, Dictionary<string, string> filter = null)
        {
            //"api/v1/jobs/lastmodifed"
            string queryParams = CreateQueryParams(studyGroups, null, filter);
            string adress = Adress + "/" + "lastmodified" + queryParams;
            System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData - adress: " + adress);
            Uri url = new Uri(adress);
            System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData - url.ToString: " + url.ToString());

            var client = new HttpClient();
            DbStudent dbStudent = new DbStudent();
            string accessToken = dbStudent.GetStudentAccessToken();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Authenticater.Authorized = false;
                return null;
            }
            System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData - bearer: " + accessToken);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
            string jsonString = null;
            try
            {
                var response = await client.GetAsync(url);
                System.Diagnostics.Debug.WriteLine("CheckServerForNewData response " + response.StatusCode.ToString());
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    System.Diagnostics.Debug.WriteLine("JobsController - CheckServerForNewData failed due to lack of Authorization");
                    Authenticater.Authorized = false;
                }

                else if (response.StatusCode == HttpStatusCode.OK) { 
                    //results = await response.Content.ReadAsAsync<IEnumerable<Job>>();
                    jsonString = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: await client.GetAsync(\"url\") Failed");
                System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: Exception msg: " + e.Message);
                System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: Stack Trace: \n" + e.StackTrace);
                System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: End Of Stack Trace");
                return null;
            }
            if (jsonString != null)
            {
                // using <string, object> instead of <string, string> makes the date be stored in the right format when using .ToString()
                Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

                if (dict.ContainsKey("uuid") && dict.ContainsKey("modified") && dict.ContainsKey("hash") && dict.ContainsKey("amountOfJobs"))
                {
                    string uuid = dict["uuid"].ToString();
                    DateTime dateTime = (DateTime)dict["modified"];
                    long modified = long.Parse(dateTime.ToString("yyyyMMddHHmmss"));
                    string uuids = dict["hash"].ToString();
                    int amountOfJobs = 0;
                    try
                    {
                        amountOfJobs = Int32.Parse(dict["amountOfJobs"].ToString());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: await client.GetAsync(\"url\") Failed");
                        System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: Exception msg: " + ex.Message);
                        System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: Stack Trace: \n" + ex.StackTrace);
                        System.Diagnostics.Debug.WriteLine("JobController - CheckServerForNewData: End Of Stack Trace");
                        return null;
                    }
                    DbJob db = new DbJob();
                    bool existInDb = db.ExistsInDb(uuid, modified);
                    if (!existInDb)
                    {
                        return "newData";
                        //return existInDb;
                    }
                    var localJobs = db.GetJobsFromDbBasedOnFilter(studyGroups, filter, true);
                    int localDbCount = localJobs.Count();

                    StringBuilder sb = new StringBuilder();
                    foreach (var job in localJobs)
                    {
                        sb.Append(job.uuid);
                    }
                    string localUuids = Hasher.CalculateMd5Hash(sb.ToString());
                    
                    // if there is a greater amount of jobs on that search filter then the job that exist 
                    // in the database has been inserted throught another search filter
                    if (uuids != localUuids)
                    {
                        if (amountOfJobs > localDbCount)
                        {
                            return "newData"; 
                            //return !existInDb;
                        }
                        return "incorrectCache";
                    }
                    return "exists";
                    //return existInDb;
                }
            }
            return null;
        }



        /// <summary>
        /// Updates the Job from the servers REST Api.
        /// 
        /// This implementation also get the minimum data from the related
        /// Companies to build a proper notification list.
        /// </summary>
        /// <param name="uuid"></param>
        public async Task UpdateJobFromServer(string uuid)
        {
            System.Diagnostics.Debug.WriteLine("JobController - UpdateJobFromServer(string uuid): initiated");
            string adress = Adress + "/" + uuid;
            System.Diagnostics.Debug.WriteLine("UpdateJobFromServer: var url = " + adress);

            Uri url = new Uri(adress);
            var client = new HttpClient();
            System.Diagnostics.Debug.WriteLine("JobController - UpdateJobFromServer: HttpClient created");
            DbStudent dbStudent = new DbStudent();

            string accessToken = dbStudent.GetStudentAccessToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Authenticater.Authorized = false;
                return;
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
            string jsonString = null;
            try
            {
                var response = await client.GetAsync(url);
                System.Diagnostics.Debug.WriteLine("UpdateJobFromServer response " + response.StatusCode.ToString());
                //results = await response.Content.ReadAsAsync<IEnumerable<Job>>();
                jsonString = await response.Content.ReadAsStringAsync();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(
                    "JobController - UpdateJobFromServer: await client.GetAsync(\"url\") Failed");
                System.Diagnostics.Debug.WriteLine("JobController - UpdateJobFromServer: Exception msg: " + e.Message);
                System.Diagnostics.Debug.WriteLine("JobController - UpdateJobFromServer: Stack Trace: \n" + e.StackTrace);
                System.Diagnostics.Debug.WriteLine("JobController - UpdateJobFromServer: End Of Stack Trace");
                return;
            }
            Deserialize(jsonString);
        }

        /// <summary>
        /// Creates the query parameters used in url to get filtered and sorted data
        /// </summary>
        private string CreateQueryParams(List<string> studyGroups = null,
            string sortBy = "", Dictionary<string, string> filter = null)
        {
            StringBuilder queryParams = new StringBuilder();
            if (studyGroups != null)
            {
                System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - studyGroups.Count(): " + studyGroups.Count());
                for (int i = 0; i < studyGroups.Count(); i++)
                {
                    if (i == 0)
                    {
                        queryParams.Append("?studygroups=");
                    }

                    else
                    {
                        queryParams.Append("&studygroups=");
                    }
                    queryParams.Append(Hasher.Base64Decode(studyGroups[i]));
                }
            }

            if (filter != null)
            {
                System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - amount of filters: " + filter.Keys.ToArray().Length);
                foreach (var category in filter.Keys.ToArray())
                {
                    if (string.IsNullOrWhiteSpace(queryParams.ToString()))
                    {
                        queryParams.Append("?");
                    }
                    else queryParams.Append("&");
                    string value = filter[category];
                    if (category != "titles")
                    {
                        value = Hasher.Base64Decode(value);
                    }
                    else {
                        // removes whitespaces from a potential user typed parameters like title search.
                        // And replaces them with +
                        value = value.Replace(" ", "+");
                    }
                    queryParams.Append(category);
                    queryParams.Append("=");
                    queryParams.Append(value);
                }
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (string.IsNullOrWhiteSpace(queryParams.ToString()))
                {
                    queryParams.Append("?");
                }
                else queryParams.Append("&");
                queryParams.Append("sortby=");
                queryParams.Append(sortBy);
            }
            return queryParams.ToString();
        }

        /// <summary>
        /// Gets a job based on optional filters.
        /// </summary>
        /// <param name="studyGroups">studyGroups can be a list of numerous studygroups ex: helse, idrettsfag, datateknologi </param>
        /// </param>
        /// <param name="filter">A dictionary where key can be: titles (values:title of the job), types (values: deltid, heltid, etc...),
        ///                      locations (values: vestagder, austagder), . 
        ///                      Supports only 1 key at this current implementation!</param>
        /// <returns></returns>
        public async Task<IEnumerable<Job>> GetJobsBasedOnFilter(List<string> studyGroups = null,
             Dictionary<string, string> filter = null)
        {
            DbJob db = new DbJob();
            //string adress = "http://kompetansetorgetserver1.azurewebsites.net/api/v1/jobs";
            string instructions = await CheckServerForNewData(studyGroups, filter);
            if (!Authenticater.Authorized)
            {
                return null;
            }
            if (instructions != null)
            {
                System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - instructions: " + instructions);
                if (instructions == "exists")  // "newData"; incorrectCache exists
                {
                    IEnumerable<Job> filteredJobs = db.GetJobsFromDbBasedOnFilter(studyGroups, filter);
                    filteredJobs = db.GetAllCompaniesRelatedToJobs(filteredJobs.ToList());
                    return filteredJobs;
                }
            }

            DbStudent dbStudent = new DbStudent();
            
            string accessToken = dbStudent.GetStudentAccessToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Authenticater.Authorized = false;
                return null;
            }

            string sortBy = "publish";
            string queryParams = CreateQueryParams(studyGroups, sortBy, filter);
            Uri url = new Uri(Adress + queryParams);
            System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - url: " + url.ToString());
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

            string jsonString = null;
            IEnumerable<Job> jobs = null;
            try
            {
                var response = await client.GetAsync(url).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter response " + response.StatusCode.ToString());
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    System.Diagnostics.Debug.WriteLine("StudentsController - UpdateStudyGroupStudent failed due to lack of Authorization");
                    Authenticater.Authorized = false;
                    
                }

                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    jsonString = await response.Content.ReadAsStringAsync();
                    //DeleteJobs(GetJobsFromDbBasedOnFilter(studyGroups, filter));
                    if (instructions != null && instructions == "incorrectCache")
                    {
                        var cachedJobs = db.GetJobsFromDbBasedOnFilter(studyGroups, filter);
                        jobs = DeserializeMany(jsonString);
                        // Get all jobs from that local dataset that was not in the data set provided by the server
                        // These are manually deleted jobs and have to be cleared from cache.
                        // linear search is ok because of small data set
                        var manuallyDeletedJobs = cachedJobs.Where(j => !jobs.Any(cj2 => cj2.uuid == j.uuid));
                        db.DeleteObsoleteJobs(manuallyDeletedJobs.ToList());
                    }
                    else
                    {
                        jobs = DeserializeMany(jsonString);
                    }

                }

                else
                {
                    System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - Using the local database");
                    jobs = db.GetJobsFromDbBasedOnFilter(studyGroups, filter);
                    jobs = db.GetAllCompaniesRelatedToJobs(jobs.ToList());
                }
                return jobs;
            }
            catch (Exception e)
            {
                // Hack workaround if mobil data and wifi is turned off
                try
                {
                    jobs = db.GetJobsFromDbBasedOnFilter(studyGroups, filter);
                    jobs = db.GetAllCompaniesRelatedToJobs(jobs.ToList());
                    return jobs;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("JobsController - GetJobsBasedOnFilter: await client.GetAsync(\"url\") Failed");
                    System.Diagnostics.Debug.WriteLine("JobsController - GetJobsBasedOnFilter: Exception msg: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("JobsController - GetJobsBasedOnFilter: Stack Trace: \n" + ex.StackTrace);
                    System.Diagnostics.Debug.WriteLine("JobsController - GetJobsBasedOnFilter: End Of Stack Trace");
                    System.Diagnostics.Debug.WriteLine("GetJobsBasedOnFilter - Using the local database");
                    return null;
                }
            }
        }

        /// <summary>
        /// Deserializes a json formated string containing multiple Project objects
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        private IEnumerable<Job> DeserializeMany(string jsonString)
        {
            System.Diagnostics.Debug.WriteLine("JobsController - DeserializeMany Initialized");

            List<object> serializedJobs =
                JsonConvert.DeserializeObject<List<object>>(jsonString);
            //System.Diagnostics.Debug.WriteLine("ProjectController - jsonString: " + jsonString);

            //List<string> serializedProjects =
            //    JsonConvert.DeserializeObject<List<string>>(jsonString);

            System.Diagnostics.Debug.WriteLine("JobsController - serializedJobs.Count(): " + serializedJobs.Count());

            List<Job> jobs = new List<Job>();
            foreach (var serializedJob in serializedJobs)
            {
                jobs.Add(Deserialize(serializedJob.ToString()));
            }
            return jobs;
        }

        /// <summary>
        /// Deseriliazes a singular Jobs with childrem. 
        /// This method is not fully completed and should be used with caution.
        /// </summary>
        /// <param name="jsonString">Serialized data contain information about job and its children</param>
        /// <returns>A deserialized Jobs object</returns>
        private Job Deserialize(string jsonString)
        {
            DbJob db = new DbJob();
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            System.Diagnostics.Debug.WriteLine("DeserializeApiData. Printing Key Value:");

            string[] keys = dict.Keys.ToArray();

            Job j = new Job();
            j.companies = new List<Company>();
            j.jobTypes = new List<JobType>();
            j.locations = new List<Location>();
            j.studyGroups = new List<StudyGroup>();           

            CompaniesController cp = new CompaniesController();

            foreach (var key in keys)
            {
                System.Diagnostics.Debug.WriteLine("key: " + key);
                System.Diagnostics.Debug.WriteLine("value: " + dict[key].ToString());
                /*
                if (!key.Equals("companies") || !key.Equals("courses") || !key.Equals("degrees")
                    || !key.Equals("jobTypes") || !key.Equals("studyGroup")) {} */
                if (key.Equals("uuid"))
                {
                    j.uuid = dict[key].ToString();
                }
                if (key.Equals("title"))
                {
                    j.title = dict[key].ToString();
                }

                if (key.Equals("description"))
                {
                    j.description = dict[key].ToString();
                }
                
                if (key.Equals("webpage"))
                {
                    j.webpage = dict[key].ToString();
                }

                if (key.Equals("expiryDate"))
                {
                    DateTime dateTime = (DateTime)dict[key];
                    j.expiryDate = long.Parse(dateTime.ToString("yyyyMMddHHmmss"));
                }

                if (key.Equals("modified"))
                {
                    DateTime dateTime = (DateTime)dict[key];
                    j.modified = long.Parse(dateTime.ToString("yyyyMMddHHmmss"));
                }
            
                
                if (key.Equals("published"))
                {
                    DateTime dateTime = (DateTime)dict[key];
                    j.published = long.Parse(dateTime.ToString("yyyyMMddHHmmss"));
                }
        

                if (key.Equals("companies"))
                {
                    CompaniesController cc = new CompaniesController();
                    DbCompany dbCompany = new DbCompany();
                    IEnumerable companies = (IEnumerable)dict[key];
                    //Newtonsoft.Json.Linq.JArray'
                    System.Diagnostics.Debug.WriteLine("companies created");
                    foreach (var comp in companies)
                    {
                        System.Diagnostics.Debug.WriteLine("foreach initiated");
                        Dictionary<string, object> companyDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(comp.ToString());
                        Company company = cc.DeserializeCompany(companyDict);
                        System.Diagnostics.Debug.WriteLine("DeserializeOneJobs: company.id: " + company.id);
                        j.companies.Add(company);
                        dbCompany.UpdateCompany(company);
                        System.Diagnostics.Debug.WriteLine("DeserializeOneJobs: After j.companies.Add(company)");
                        string jobUuid = dict["uuid"].ToString();
                        dbCompany.InsertCompanyJob(company.id, jobUuid);
                    }
                }

                if (key.Equals("studyGroups"))
                {
                    IEnumerable studyGroups = (IEnumerable)dict[key];
                    //Newtonsoft.Json.Linq.JArray'
                    System.Diagnostics.Debug.WriteLine("studyGroups created");
                    DbStudyGroup dbStudyGroup = new DbStudyGroup();
                    foreach (var studyGroup in studyGroups)
                    {
                        System.Diagnostics.Debug.WriteLine("foreach initiated");
                        Dictionary<string, object> studyGroupDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(studyGroup.ToString());
                        
                        StudyGroup sg = new StudyGroup();
                        if (studyGroupDict.ContainsKey("id"))
                        {
                            sg.id = studyGroupDict["id"].ToString();
                        }

                        if (studyGroupDict.ContainsKey("name"))
                        {
                            sg.name = studyGroupDict["name"].ToString();
                        }

                        j.studyGroups.Add(sg);

                        string jobUuid = dict["uuid"].ToString();
                        dbStudyGroup.InsertStudyGroupJob(sg.id, jobUuid);

                    }
                }

                if (key.Equals("locations"))
                {
                    IEnumerable locations = (IEnumerable)dict[key];
                    //Newtonsoft.Json.Linq.JArray'
                    DbLocation dbLocation = new DbLocation();
                    System.Diagnostics.Debug.WriteLine("location created");
                    foreach (var location in locations)
                    {
                        System.Diagnostics.Debug.WriteLine("foreach initiated");
                        Dictionary<string, object> locationDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(location.ToString());
                        Location loc = new Location();
                        if (locationDict.ContainsKey("id"))
                        {
                            
                            loc.id = locationDict["id"].ToString();
                            System.Diagnostics.Debug.WriteLine("location id: " + loc.id);
                        }

                        if (locationDict.ContainsKey("name"))
                        {
                            loc.name = locationDict["name"].ToString();
                        }

                        dbLocation.InsertLocation(loc);
                        j.locations.Add(loc);
                        string jobUuid = dict["uuid"].ToString();
                        dbLocation.InsertLocationJob(loc.id, jobUuid);
                    }
                }


                if (key.Equals("jobTypes"))
                {
                    IEnumerable jobTypes = (IEnumerable)dict[key];
                    //Newtonsoft.Json.Linq.JArray'
                    DbJobType dbJobType = new DbJobType();
                    System.Diagnostics.Debug.WriteLine("jobTypes created");
                    foreach (var jobType in jobTypes)
                    {
                        System.Diagnostics.Debug.WriteLine("foreach initiated");
                        Dictionary<string, object> jtDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jobType.ToString());

                        JobType jt = new JobType();
                        if (jtDict.ContainsKey("id"))
                        {
                            jt.id = jtDict["id"].ToString();
                        }

                        if (jtDict.ContainsKey("name"))
                        {
                            jt.name = jtDict["name"].ToString();
                        }

                        dbJobType.InsertJobType(jt);

                        System.Diagnostics.Debug.WriteLine("before j.jobTypes.Add(jt);");
                        j.jobTypes.Add(jt);

                        string jobUuid = dict["uuid"].ToString();
                        dbJobType.InsertJobTypeJob(jt.id, jobUuid);
                    }
                }
            }
            db.UpdateJob(j);
            return j;
        }
    }
}



