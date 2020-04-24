'use strict';

/**
 * @ngdoc function
 * @name srcApp.sections
 * @description
 * # sections
 * Returns the list of sections
 */
angular.module('srcApp')
  .factory('sections', function() {
    return function() {
      return [
      { 'Title': 'Out of Character',
        'Glyph': 'glyphicon-ok',
        'Url': '/outofcharacter',
        'Articles': [
          { 'Title': 'A Brief Guide to History',
            'Content': '',
            'PdfUrl': '/media/A+Brief+Guide+to+History.pdf'},
          { 'Title': 'Companies & Corporations',
            'Content': '',
            'PdfUrl': '/media/Companies+%26+Corporations.pdf'},
          { 'Title': 'Groups & Organisations',
            'Content': '',
            'PdfUrl': '/media/Groups+%26+Organisations.pdf'},
          { 'Title': 'Planetary Database',
            'Content': '',
            'PdfUrl': '/media/Planetary+Database.pdf'},
          { 'Title': 'Safety Rules & Game Play Guidelines',
            'Content': '',
            'PdfUrl': '/media/Out-Of-Character/Safety+Rules+%26+Game+Play+Guidelines.pdf'},
          { 'Title': 'The Writer\'s Guide',
              'Content': '',
              'PdfUrl': '/media/Writer%27s+Guide.pdf'},
          { 'Title': 'Bill Chiver\'s Training Ideas',
            'Content': '',
            'PdfUrl': '/media/Out-Of-Character/Bill+Chivers%27s+Training+Ideas.pdf'},
            ]
        },

        { 'Title': 'Miscellanea',
        'Glyph': 'glyphicon-ok',
        'Url': '/miscellanea',
        'Articles': [
          { 'Title': 'Prop Designs',
            'Content': '',
            'PdfUrl': '/media/Miscellanea/Prop-Designs.pdf'},
            { 'Title': 'Scenario Summaries (for 93-94)',
            'Content': '',
            'PdfUrl': '/media/Miscellanea/Scenario-Summaries-93-94.pdf'},
          { 'Title': 'Welcome to the Marie Celesta - Scenario Notes',
            'Content': '',
            'PdfUrl': '/media/Miscellanea/Welcome-to-the-Marie-Celeste.pdf'},
          { 'Title': 'One of our Drones is Missing - Scenario Notes',
            'Content': '',
            'PdfUrl': '/media/Miscellanea/One-Of-Our-Drones-Is-Missing.pdf'},
          { 'Title': 'How Much for just the Planet - Scenario Notes',
            'Content': '',
            'PdfUrl': '/media/Miscellanea/How-Much-For-Just-The-Planet.pdf'},
            { 'Title': 'Drive Wars - Scenario Notes',
            'Content': '',
            'PdfUrl': '/media/Drive-Wars.pdf'},
            { 'Title': 'Drive Wars II - Scenario Notes',
            'Content': '',
            'PdfUrl': '/media/Drive-Wars-II.pdf'},
            ]
        },

        { 'Title': 'In-Character',
        'Glyph': 'glyphicon-book',
        'Url': '/incharacter',
        'Articles': [
        { 'Title': 'Briefing Notes - Mission "Hydra"',
          'Content': '',
          'PdfUrl': '/media/In-Character/Briefing+Notes+-+Mission+%22Hydra%22.pdf'},
        { 'Title': 'Briefing Notes - Mission "Vortex"',
          'Content': '',
          'PdfUrl': '/media/In-Character/Briefing+Notes+-+Mission+%22Vortex%22.pdf'},
        { 'Title': 'Briefing Notes - Type 12 Experimental Station',
          'Content': '',
          'PdfUrl': '/media/In-Character/Briefing+Notes+-+Type+12+Experimental+Station.pdf'},
        { 'Title': 'Briefing Notes - Type 23 Exo-Suit',
          'Content': '',
          'PdfUrl': '/media/In-Character/Briefing+Notes+-+Type+23+Exo-Suit.pdf'},
        { 'Title': 'Comms Message - 2495-11-09 - Martins to Turner (Mission "Intruder")',
          'Content': '',
          'PdfUrl': '/media/In-Character/Message+2495-11-09+Martins+to+Turner+-+Mission+%22Intruder%22.pdf'},
        { 'Title': 'Comms Message - 2496-01-28 - Calvin to Turner (Mission "Intruder")',
          'Content': '',
          'PdfUrl': '/media/In-Character/Message+2496-01-28+Calvin+to+Turner+-+Mission+%22Intruder%22.pdf'},
        { 'Title': 'Personnel Jacket - Turner J (TJ121710)',
          'Content': '',
          'PdfUrl': '/media/In-Character/Personnel+Jacket+-+Turner+J+TJ121710.pdf'},
        { 'Title': 'Tactical Operations Centre - Terms of Reference',
          'Content': '',
          'PdfUrl': '/media/In-Character/Tactical+Operations+-+Terms+of+Reference.pdf'},
        ]
      },

      { 'Title': 'Mexal\'s Letters Home',
        'Glyph': 'glyphicon-envelope',
        'Url': '/mexalsletters',
        'Articles': [
          { 'Title': '2493-03 - Kruger',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2493-03++-+Kruger.pdf'},
          { 'Title': '2494-06 - Laverii-426',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2493-06++-+Laverii-426.pdf'},
          { 'Title': '2494-09 - X244',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2493-09+-+X244.pdf'},
          { 'Title': '2494-12 - Juliette',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2493-12+-+Juliette.pdf'},
          { 'Title': '2495-01 - Grant\'s World',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-01+-+Grant%27s+World.pdf'},
          { 'Title': '2495-02 - Heurls',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-02+-+Heurls.pdf'},
          { 'Title': '2495-03 - Connet',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-03+-+Connet.pdf'},
          { 'Title': '2495-04 - Groombridge',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-04+-+Groombridge.pdf'},
          { 'Title': '2495-05 - Marie Celesta',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-05+-+Marie+Celesta.pdf'},
          { 'Title': '2495-07 - Hal',
            'Content': '',
            'PdfUrl': '/media/Mexal%27s+Letters/2495-07+-+Hal.pdf'},
            ]},

      { 'Title': 'Mission Reports',
        'Glyph': 'glyphicon-screenshot',
        'Url': '/missionreports',
        'Articles': [
            { 'Title': '2494-03 - Mission "Sidewinder" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2494-03+Mission+%22Sidewinder%22+(Calvin).pdf'},
            { 'Title': '2494-04 - Mission "Ogre" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2494-04+Mission+%22Ogre%22+(Calvin).pdf'},
            { 'Title': '2494-06 - Mission "Pegasus" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2494-06+Mission+%22Pegasus%22+(Calvin).pdf'},
            { 'Title': '2495-01 - Mission "-C-L-A-S-S-I-F-I-E-D-" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2494-12+Mission+%22-C-L-A-S-S-I-F-I-E-D%22+(Calvin).pdf'},
            { 'Title': '2494-03 - Mission "Nightside" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2495-01+Mission+%22Nightside%22+(Calvin).pdf'},
            { 'Title': '2495-05 - Boarding Report "SS Marie Celesta" (Oslo).',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2495-05+Boarding+Report+%22SS+Marie+Celesta%22+(Oslo).pdf'},
            { 'Title': '2494-03 - Mission "Omaha" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2495-07+Mission+%22Omaha%22+(Calvin).pdf'},
            { 'Title': '2494-03 - Mission "Nova" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2495-11+Mission+%22Nova%22+(Calvin).pdf'},
            { 'Title': '2494-03 - Mission "Intruder" (Calvin)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2496-02+Mission+%22Intruder%22+(Calvin).pdf'},
            { 'Title': '2494-03 - Mission "Intruder" - SIB Report (Rumpole)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2496-02+Mission+%22Intruder%22+-+SIB+Report+(Rumpole).pdf'},
            { 'Title': '2494-03 - Mission "Red Storm" (Turner)',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2496-03+Mission+%22Red+Storm%22+(Turner).pdf'},
            { 'Title': '2496-00 - Mission "Procyon" - Intel',
              'Content': '',
              'PdfUrl': '/media/Mission+Reports/2496-00+Mission+%22Procyon%22+-+Intel.pdf'},
        ]},

      { 'Title': 'Marine\'s Handbook',
        'Glyph': 'glyphicon-book',
        'Url': '/marineshandbook',
        'Articles': [
          { 'Title': 'Contents',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Contents.pdf'},
          { 'Title': 'Basic Combat Training',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Basic+Combat+Training.pdf'},
          { 'Title': 'Patrolling',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Patrolling.pdf'},
          { 'Title': 'Scouting',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Scouting.pdf'},
          { 'Title': 'Sniper',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Sniper.pdf'},
          { 'Title': 'Med-Tech 4',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+MedTech+4.pdf'},
          { 'Title': 'Med-Tech 3',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+MedTech+3.pdf'},
          { 'Title': 'Med-Tech 2',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+MedTech+2.pdf'},
          { 'Title': 'Med-Tech 1',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+MedTech+1.pdf'},
          { 'Title': 'EOD Specialist 4',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+EOD+Specialist+4.pdf'},
          { 'Title': 'Armourer 4',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Armourer+4.pdf'},
          { 'Title': 'Radio Operator 2',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+Radio+Operator+2.pdf'},
          { 'Title': 'CompTech - Course Overview',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+CompTech+-+Course+Overview.pdf'},
          { 'Title': 'CompTech 3',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+CompTech+3.pdf'},
          { 'Title': 'CompTech 2',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+CompTech+2.pdf'},
          { 'Title': 'NCO & Officer Training - Abridged',
            'Content': '',
            'PdfUrl': '/media/Marine%27s+Handbook/Marine%27s+Handbook+-+NCO+%26+Officer+Training+-+Abridged.pdf'},

                    ]},
    ]; };
  });
