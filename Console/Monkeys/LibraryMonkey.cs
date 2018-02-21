using System.Linq;
using Octopus.Client;
using Octopus.Client.Model;
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Octopus.Client.Model.Accounts;
using Serilog;

namespace SeaMonkey.Monkeys
{
    public class LibraryMonkey : Monkey
    {
        public LibraryMonkey(OctopusRepository repository) : base(repository)
        {
        }

        public void CreateRecords(int numberOfFeeds,
            int numberOfScriptModules,
            int numberOfLibraryVariableSets,
            int numberOfTenantTagSets)
        {
            CreateFeeds(numberOfFeeds);
            CreateScriptModules(numberOfScriptModules);
            CreateLibraryVariableSets(numberOfLibraryVariableSets);
            CreateTenantTagSets(numberOfTenantTagSets);
            CreateAcmePackages(10);
        }

        #region Feeds

        public void CreateFeeds(int numberOfRecords)
        {
            var currentCount = Repository.Feeds.FindAll().Count();
            for (var x = currentCount; x <= numberOfRecords; x++)
                CreateFeed(x);
        }

        private FeedResource CreateFeed(int prefix)
        {
            return
                Repository.Feeds.Create(new NuGetFeedResource()
                {
                    Name = "Feed-" + prefix.ToString("000"),
                    FeedUri = "https://api.nuget.org/v3/index.json",
                });
        }

        #endregion

        #region ScriptModule

        public void CreateScriptModules(int numberOfRecords)
        {
            var currentCount = Repository.LibraryVariableSets.FindAll().Count();
            for (var x = currentCount; x <= numberOfRecords; x++)
                CreateScriptModule(x);
        }

        private LibraryVariableSetResource CreateScriptModule(int prefix)
        {
            return
                Repository.LibraryVariableSets.Create(new LibraryVariableSetResource()
                {
                    Name = "LibraryVariableSet-" + prefix.ToString("000"),
                    Description = "Let's get schwifty!",
                    ContentType = VariableSetContentType.ScriptModule,
                });
            //TODO: write an actual PowerShell script with the VariableSetId that comes back from this request.
        }

        #endregion

        #region LibraryVariableSets

        public void CreateLibraryVariableSets(int numberOfRecords)
        {
            var currentCount = Repository.LibraryVariableSets.FindAll().Count();
            for (var x = currentCount; x <= numberOfRecords; x++)
                CreateLibraryVariableSet(x);
        }

        private LibraryVariableSetResource CreateLibraryVariableSet(int prefix)
        {
            return
                Repository.LibraryVariableSets.Create(new LibraryVariableSetResource()
                {
                    Name = "LibraryVariableSet-" + prefix.ToString("000"),
                    Description =
                        "Rick: Uh-huh, yeah, that’s the difference between you and me, Morty. I never go back to the carpet store"
                });
        }

        #endregion

        #region TenantTagSets

        public void CreateTenantTagSets(int numberOfRecords)
        {
            var currentCount = Repository.TagSets.FindAll().Count();
            for (var x = currentCount; x <= numberOfRecords; x++)
                CreateTenantTagSet(x);
        }

        private TagSetResource CreateTenantTagSet(int prefix)
        {
            return
                Repository.TagSets.Create(new TagSetResource()
                {
                    Name = "TenantTagSet-" + prefix.ToString("000"),
                    Description =
                        "Listen, Morty, I hate to break it to you but what people call 'love' is just a chemical reaction that compels animals to breed. It hits hard, Morty, then it slowly fades, leaving you stranded in a failing marriage. I did it. Your parents are gonna do it. Break the cycle, Morty. Rise above. Focus on science"
                });
        }

        #endregion

        public void CreateAcmePackages(int numberOfPackages)
        {
            var filename = Path.GetTempFileName();
            try
            {
                var packages = Repository.BuiltInPackageRepository.ListPackages("Acme.Web", take: int.MaxValue);
                using (var ms = new MemoryStream(GetRandomZipFile()))
                {
                    for (var x = packages.Items.Count; x <= numberOfPackages; x++)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        var packageId = $"Acme.Web.1.{x}.0.zip";
                        Log.Information("Pushing package {package}", packageId);
                        Repository.BuiltInPackageRepository.PushPackage(packageId, ms);
                    }
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        private byte[] GetRandomZipFile()
        {
            var rnd = new Random();

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipOutputStream(ms))
                {
                    zip.PutNextEntry(new ZipEntry("file"));
                    var buffer = new byte[10000];
                    rnd.NextBytes(buffer);
                    zip.Write(buffer, 0, buffer.Length);
                    zip.Flush();
                    return ms.ToArray();
                }
            }
        }
    }
}