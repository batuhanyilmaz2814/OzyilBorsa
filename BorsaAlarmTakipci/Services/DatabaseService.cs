using SQLite;
using System.IO;
using BorsaAlarmTakipci.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BorsaAlarmTakipci.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private bool _initialized = false;

        // Veri tabanı dosyamızın adı:
        private const string DatabaseFilename = "BorsaAlarmSQLite.db3";

        //Veritabanı dosyamızın yolu;
        private string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        // Constructor

        public DatabaseService()
        {
            
        }

        // Veritabanını başlatma

        private async Task InitializeAsync()
        {
            if (_initialized) return;

            _database = new SQLiteAsyncConnection(DatabasePath);

            await _database.CreateTableAsync<AlarmDefinition>();

            _initialized = true;
        }

        //Tüm tanımları getiren metot

        public async Task<List<AlarmDefinition>> GetAlarmsAsync()
        {
            await InitializeAsync();
            return await _database.Table<AlarmDefinition>().ToListAsync();

        }

        // ID'ye göre alarm tanımını getiren metot

        public async Task<AlarmDefinition> GetAlarmAsync(int id)
        {
            await InitializeAsync();
            return await _database.Table<AlarmDefinition>().Where(i => i.Id == id).FirstOrDefaultAsync();

        }
        // Yeni alarm tanımı ekleyen metot

        public async Task<int> SaveAlarmAsync(AlarmDefinition alarm)
        {
            await InitializeAsync();
            if (alarm.Id != 0) // eğer id varsa güncelleme yapılır
            {
                return await _database.UpdateAsync(alarm);
            }
            else
            // eğer id yoksa yeni kayıt eklenir
            {
                return await _database.InsertAsync(alarm);
            }

        }

        //Bir alarmı silen metot

        public async Task<int> DeleteAlarmAsync(AlarmDefinition alarm)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(alarm);

        }

        // DatabaseService.cs içinde

        // ... (diğer metotlar)

        public async Task<List<AlarmDefinition>> GetActiveAlarmsAsync()
        {
            await InitializeAsync();
            return await _database.Table<AlarmDefinition>().Where(a => a.IsEnabled).ToListAsync();
        }




    }
}
