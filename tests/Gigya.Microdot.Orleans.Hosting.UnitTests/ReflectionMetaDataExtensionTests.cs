﻿#region Copyright 
// Copyright 2017 Gigya Inc.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.  
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gigya.Microdot.SharedLogic.Events;
using NUnit.Framework;
using Gigya.ServiceContract.Attributes;
using Shouldly;

namespace Gigya.Microdot.Orleans.Hosting.UnitTests
{
    [TestFixture]
    public class ReflectionMetaDataExtensionTests
    {
        private int _numOfProperties;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _numOfProperties = typeof(PersonMockData).GetProperties().Length;
        }

        [Test]
        public void GetProperties_Extract_All_Public_Properties()
        {
            var mock = new PersonMockData();
            var reflectionMetadataInfos = MetadataPropertiesCache.ExtracMetadata<PersonMockData>().ToList();

            reflectionMetadataInfos.Count.ShouldBe(_numOfProperties);

            foreach (var reflectionMetadata in reflectionMetadataInfos)
            {
                var propertyInfo = typeof(PersonMockData).GetProperty(reflectionMetadata.PropertyName);

                var result = reflectionMetadata.ValueExtractor(mock);

                if (propertyInfo.GetValue(mock).Equals(result) == false)
                {
                    throw new InvalidDataException($"Propery name {propertyInfo.Name} doesn't exists.");
                }

            }
        }


        [Test]
        public void GetProperties_Extract_Sensitive_Attribute()
        {
            const string crypticPropertyName = nameof(PersonMockData.Cryptic);
            const string sensitivePropertyName = nameof(PersonMockData.Sensitive);

            var cache = new MetadataPropertiesCache();
            var mock = new PersonMockData();
            var @params = cache.ParseIntoParams(mock);

            foreach (var metadataInfo in @params.Where(x => x.Sensitivity != null))
            {
                if (metadataInfo.Name == crypticPropertyName)
                {
                    metadataInfo.Sensitivity.ShouldBe(Sensitivity.Secretive);
                    typeof(PersonMockData).GetProperty(crypticPropertyName).GetValue(mock).ShouldBe(mock.Cryptic);
                }

                if (metadataInfo.Name == sensitivePropertyName)
                {
                    metadataInfo.Sensitivity.ShouldBe(Sensitivity.Sensitive);
                    typeof(PersonMockData).GetProperty(sensitivePropertyName).GetValue(mock).ShouldBe(mock.Sensitive);

                }
            }
        }



        [Test]
        public void CacheMetadata_Extract_All_Public_Properties()
        {
            var cache = new MetadataPropertiesCache();
            var mock = new PersonMockData();
            var @params = cache.ParseIntoParams(mock).ToList();

            @params.Count.ShouldBe(_numOfProperties);
            foreach (var param in @params)
            {
                var propertyInfo = typeof(PersonMockData).GetProperty(param.Name);

                if (propertyInfo.GetValue(mock).ToString().Equals(param.Value.ToString()) == false)
                {
                    throw new InvalidDataException($"Propery name {propertyInfo.Name} doesn't exists.");
                }
            }
        }

        [Test]
        public void CacheMetadata_Strength_Test()
        {
            var cache = new MetadataPropertiesCache();

            var people = GeneratePeople(10000).ToList();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var person in people)
            {
                var @params = cache.ParseIntoParams(person).ToList();

                @params.Count.ShouldBe(_numOfProperties);
            }
            stopWatch.Stop();
        }

        private IEnumerable<PersonMockData> GeneratePeople(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                yield return new PersonMockData { ID = i, Name = "Name", Cryptic = true };
            }
        }
    }

    internal class PersonOperationMock
    {
        public void PrintPerson([LogFields] PersonMockData person, PersonMockData person2, PersonMockData person3)
        {

        }

    }


    internal class PersonMockData
    {
        public int ID { get; set; } = 10;

        public string Name { get; set; } = "Mocky";

        public bool IsMale { get; set; } = false;

        [Sensitive(Secretive = false)]

        public bool Sensitive { get; set; } = true;

        [Sensitive(Secretive = true)]

        public bool Cryptic { get; set; } = true;

    }

}

