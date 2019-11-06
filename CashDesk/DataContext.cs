using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace CashDesk
{
	public class DataContext: DbContext
	{

		public DbSet<Deposit> Deposits { get; set; }

		public DbSet<Member> Members { get; set; }

		public DbSet<Membership> Memberships { get; set; }

		

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseInMemoryDatabase("CashDesk");
		}
	}
}
