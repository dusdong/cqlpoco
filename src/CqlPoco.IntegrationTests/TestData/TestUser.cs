﻿using System;

namespace CqlPoco.IntegrationTests.TestData
{
    /// <summary>
    /// Test user data in C*.
    /// </summary>
    public class TestUser
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}