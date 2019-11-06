using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CashDesk
{

	public class DataAccess : IDataAccess
	{
		private DataContext dataContext;

		private void ThrowIfNotInitialized()
		{

			if (dataContext == null)
			{
				throw new InvalidOperationException("Not initialized");
			}
		}


		public async Task InitializeDatabaseAsync()
		{
			if (dataContext != null)
			{
				throw new InvalidOperationException("Datacontext is already initialized!");
			}

			dataContext = new DataContext();
			return Task.CompletedTask;

		}



		public async Task<int> AddMemberAsync(string firstName, string lastName, DateTime birthday)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) || birthday == null)
			{
				throw new ArgumentException("Invalid argument! Must not be null or empty!");
			}

			//check if the member exists already
			foreach (Member m in dataContext.Members)
			{
				if (m.LastName == lastName)
				{
					throw new DuplicateNameException("User exists already!");
				}
			}

			Member newM = new Member
			{
				FirstName = firstName,
				LastName = lastName,
				Birthday = birthday
			};

			dataContext.Add(newM);
			await dataContext.SaveChangesAsync();

			return newM.MemberNumber;
		}


		/// <inheritdoc />
		public async Task DeleteMemberAsync(int memberNumber)
		{
			ThrowIfNotInitialized();

			Member deleteM;
			try
			{
				foreach (Member m in dataContext.Members)
				{
					if (m.MemberNumber == memberNumber)
					{
						deleteM = m;
						dataContext.Remove(deleteM);
					}
				}
			}
			catch (InvalidOperationException)
			{
				throw new ArgumentException();
			}

			await dataContext.SaveChangesAsync();
		}




		public async Task<IMembership> JoinMemberAsync(int memberNumber)
		{
			ThrowIfNotInitialized();

			foreach (Membership m in dataContext.Memberships)
			{
				if (DateTime.Now >= m.Begin && DateTime.Now <= m.End && m.Member.MemberNumber == memberNumber)
				{
					throw new AlreadyMemberException();
				}
			}

			var newMS = new Membership
			{
				Member = await dataContext.Members.FirstAsync(m => m.MemberNumber == memberNumber),
				Begin = DateTime.Now,
				End = DateTime.MaxValue
			};

			dataContext.Memberships.Add(newMS);
			await dataContext.SaveChangesAsync();

			return newMS;
		}

		/// <inheritdoc />
		public async Task<IMembership> CancelMembershipAsync(int memberNumber)
		{
			ThrowIfNotInitialized();

			Membership ms = new Membership();
			try
			{
				foreach (Membership m in dataContext.Memberships)
				{
					if (m.Member.MemberNumber == memberNumber && m.End == DateTime.MaxValue)
					{
						ms = m;
						ms.End = DateTime.Now;

						return ms;
					}
				}
			}
			catch (InvalidOperationException)
			{
				throw new NoMemberException();
			}

			await dataContext.SaveChangesAsync();
			return ms;
		}


		/// <inheritdoc />
		public async Task DepositAsync(int memberNumber, decimal amount)
		{
			ThrowIfNotInitialized();


			Membership ms;
			try
			{
				ms = await dataContext.Memberships.FirstAsync(m => m.Member.MemberNumber == memberNumber && DateTime.Now <= m.End && DateTime.Now >= m.Begin);
			}
			catch (InvalidOperationException)
			{
				throw new NoMemberException();
			}


			Member member;
			try
			{
				member = await dataContext.Members.FirstAsync(m => m.MemberNumber == memberNumber);
			}
			catch (InvalidOperationException)
			{
				throw new ArgumentException();
			}


			var newD = new Deposit
			{
				Membership = ms,
				Amount = amount
			};

			dataContext.Deposits.Add(newD);
			await dataContext.SaveChangesAsync();


		}
		/// <inheritdoc />
		public async Task<IEnumerable<IDepositStatistics>> GetDepositStatisticsAsync()
		{
			ThrowIfNotInitialized();

			return (await dataContext.Deposits.Include("Membership.Member").ToArrayAsync())
				.GroupBy(d => new { d.Membership.Begin.Year, d.Membership.Member })
				.Select(i => new DepositStatistics
				{
					Year = i.Key.Year,
					Member = i.Key.Member,
					TotalAmount = i.Sum(d => d.Amount)
				});

		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (dataContext != null)
			{
				dataContext.Dispose();
				dataContext = null;
			}
		}
	}
}
