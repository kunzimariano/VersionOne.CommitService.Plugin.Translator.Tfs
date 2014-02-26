using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Newtonsoft.Json;
using NUnit.Framework;
using VersionOne.CommitService.Interfaces;
using VersionOne.CommitService.Types;

namespace VersionOne.CommitService.Plugin.Translator.Tfs.Tests
{
	[TestFixture]
	public class TfsTranslatorTests
	{
		private readonly ITranslateInboundMessageToCommits _translator = new TfsTranslator();
		private InboundMessage _validMessage;
		private InboundMessage _invalidMessage;

		[SetUp]
		public void SetUp()
		{
			var headers = new Dictionary<string, string[]>()
			{
				{ "User-Agent", new[] { "Team Foundation (TfsJobAgent.exe, 10.0.40219.1)" } }
			};

			var validSampleData = File.ReadAllText(@".\TestData\ValidMessage.xml");
			_validMessage = new InboundMessage(validSampleData, headers);


			var inValidSampleData = File.ReadAllText(@".\TestData\InValidMessage.xml");
			_invalidMessage = new InboundMessage(inValidSampleData, headers);
		}

		[Test]
		public void CanProcess_is_true_for_valid_message_and_useragent()
		{
			bool canProcess = _translator.CanProcess(_validMessage);
			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_invalid_useragent()
		{
			_validMessage.Headers["User-Agent"][0] = "nonsense";
			bool canProcess = _translator.CanProcess(_validMessage);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_invalid_body_message()
		{
			bool canProcess = _translator.CanProcess(_invalidMessage);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void Execute_succeeds_for_valid_message()
		{
			Translation.Result result = _translator.Execute(_validMessage);
			Assert.IsTrue(result.TranslationResult.IsRecognized);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;
			Assert.AreEqual(1, translationResult.commits.Count());
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void On_Execute_succeeds_commit_result_matches_expectation()
		{
			Translation.Result result = _translator.Execute(_validMessage);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;
			var cm = translationResult.commits.FirstOrDefault();
			Approvals.Verify(JsonConvert.SerializeObject(cm, Formatting.Indented));
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void On_Execute_succeeds_response_matches_expectation()
		{
			Translation.Result result = _translator.Execute(_validMessage);
			var content = (InboundMessageResponse.Content.Custom)result.Content;
			var stringResponse = content.Item.ReadAsStringAsync().Result;
			Approvals.Verify(stringResponse);
		}

		[Test]
		public void Execute_fails_on_non_parsable_input()
		{
			Translation.Result result = _translator.Execute(_invalidMessage);
			Assert.IsTrue(result.TranslationResult.IsFailure);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void On_Execute_fails_response_matches_expectation()
		{
			Translation.Result result = _translator.Execute(_invalidMessage);
			var content = (InboundMessageResponse.Content.Custom)result.Content;
			var stringResponse = content.Item.ReadAsStringAsync().Result;
			Approvals.Verify(stringResponse);
		}
	}
}
