using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interner_magazine
{

    internal class DatabaseConnection
    {

            private string connectionString = "Server=localhost;Port=5432;Database=Internet_magazine;User Id=postgres;Password=FreeZ231";

            public NpgsqlConnection GetConnection()
            {
                return new NpgsqlConnection(connectionString);
            }
        }
    }

