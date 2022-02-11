::: callout-security
Classified
:::

**Document No.** CMS090/1542987/A
**Course No.** CMS090/0124865-2/A
_Computer Technician, Level 2_

Course Notes

##### Prepared by:

Cpl. J. Turner
Comp-Tech 1A
C1l9/8105/5869

### 1: Introduction

This document forms the course notes for the Level 2 Computer
Technician course (Course No. CMS090/0124865-2/A), and as such is
required reading for that course.

Additional required reading are the technical manuals for the standard
1ssue personal computer (hereafter refered to as the SIPC), namely
Psion Corporation document no.s 6100-065, 6100-0068 and 6100-0071.

Evaluation of the course at this level is by a supervised practical
examination on the core subjects studied, and by written examination on
the entire syllabus. In addition, the examiner may, at his discression,
require an oral examination of any or all candidates to evaluate
understanding of the applications of the course material.

Due to the nature of some of the course content, every candidate must
hold Comms-Tech 2A or better qualification prior to acceptance on this
course.

### 2: Course Description

Comp-Tech qualification level 2 is the intermediate level course, which
centres on programming the standard issue personal computer,
fundamentals and recognition of advanced computer equipment and basic
data analysis and cryptology. Marines passing this course are cleared
to be issued and use advanced equipment, to use remote data links and
to attempt to decrypt encrypted data. High evaluation scores in Comp-
Tech 3 and Comms-Tech 2A are required for acceptance on this course.

### 3: Syllabus

- Overview of current advanced computer technology
- Hardware technology
- User interface technology
- Programming technology
- Introduction to wetware and artificially
  intelligent systems
- Advanced use of the SIPC
  - Location of available programs
  - Location and retrieval of program source
  - Transfer of programs and source between
    SIPC storage devices
  - Basic SIPC programming
  - Data transfer from SIPC via communications links
(requires Comms-Tech 2A qualification)
- Basic data analysis techniques
- Basic cryptology
  - Caesarean crypt
  - Substitution crypt
  - Numeric Substitution crypt
  - Logical Progression crypt

### 4: Course Notes

##### 4.1: Overview of current advanced computer technology

At this time, the ACT section of the course is taught as lectures
by an instructor. Thus, the course notes below consist only of
key words to be noted.

###### 4.1.1: Hardware technology

* Electronic
* Optical
* Chemical (Wetware)

###### 4.1.2: User interface technology

* Keyboard
* CRT Display
* Mouse
* Verbal input
* Optical input
*  Thought input

###### 4.1.3: Programming technology

* CAP tools
* Formal programming methods
* Self programming systems
* Neural network biasing
* Chemical (Wetware) agents

###### 4.1.4: Introduction to wetware and artificially intelligent systems

##### 4.2: Advanced use of the SIPC

All the course notes for this section of the syllabus are
contained in the SIPC technical reference manuals detailed in
section 5 below. The following sub-sections provide references to
the appropriate sections of the technical reference manuals.

###### 4.2.1: Location of Available Programs

See Psion Corporation document no. 6100-0065, section xxx
on page XXX.

###### 4.2.2: Location and Retrieval of Program Source

See Psion Corporation document no. 6100-0065, section xxx
on page XXX.

###### 4.2.3 Transfer of Programs and Source between SIPC Storage
Devices

See Psion Corporation document no. 6100-0065, section xxx
on page XXX.

###### 4.2.4: Basic SIPC Programming

For SIPC programming notes, see Psion Corporation document
no. 6100-0068.

###### 4.2.5: Data Transfer from SIPC via Communications Links

For SIPC communications link note, see Psion Corporation
document no. 6100-0071.

##### 4.3: Basic Data Analysis Techniques

The subject of data analysis techniques is vital when analysing
the contents of foreign SIPC data storage devices (be they the
internal storage device or data-packs), and leads directly
towards the higher course subjects of Security Loophole Detection
and Exploitation, commonly refered to as hacking.

This is a subject in which excelence is almost entirely up to the
user being able to make intuitive jumps to determine the
Significance of information.

In summary, the basis of data analysis for the Marine Comp-Tech
is finding links between items of information and bridging any
gaps that exist in the information set.

Some important considerations are:

- 99% of all data entered into an SIPC storage device
will be of importance to the user.

- If a password lock is in place, the password will
inevitably either mean something significant to the
user, or be written down where the user has easy
access to it. It is therefore vital that a thorough
search 1s made when an SIPC or data-pack is recovered
for any additional material. Nothing is
insignificant.

- If the data stored in an SIPC storage device is
encoded, then there must exist encoding and decoding
programs, either for an SIPC, or for a ground fixed
installation with link capability to an SIPC. It may
be possible to decrypt simple codes by hand, but for
anything more complex than a simple crypt method,
decoding without the correct program and key is
almost impossible.

- Although an entry deleted from and SIPC data pack
cannot be retrieved except with specialist ground-
fixed equipment, inferences can be drawn from obvious
omissions from sets of data.

##### 4.4: Basic cryptology

The three basic and simplest crypt methods are all variants on
the substitution method, where letters, groups of letters or
words are manipulated by substituting other groups.

For every substitution method there must exist a substitution
key, by which information may be encrypted. The decrypt method is
always the reverse of the encrypt method for substitution crypts.

It should be noted that in the crypt descriptions below, the
substitution of a letter may equally be replaced with the
substitution of a letter group or entire words.

In addition, a fourth method of encryption 1s covered in this
section, this being a partial variant on the substitution method
called a Logical Progression crypt.

###### 4.4.1: Caesarean crypt

The simplest crypt method of all to understand, the
Caesarean crypt method dates into antiquity, yet is so
simple to implement that it is still used where low-level
encryption 1s required.

In the encrypt stage, each letter of the alphabet is
substituted with the letter a given number of places
further on in the alphabet, returning to the beginning and
continuing to count after the end of the aiphabet is
reached (known as ‘rolling over’). The number of places
‘displacement’ is known as the Caesarean key. Punctuation
may or may not be substituted, anda ‘rolling key’ may be
employed, whereby a multi-figure number is used as the key,
and each figure of the number 1s in turn used as the
displacement key, starting at the first again after the
last has been used.

The decrypt method is identical, other than that each
letter is substituted with the letter a given number of

placed back in the alphabet, again rolling-over as
required.

The Caesarean crypt is highly vulnerable to letter
frequency analysis techniques, and may be broken by hand if
sufficient time and effort are employed.

###### 4.4.2: Substitution crypt

The substitution crypt 1s similar to the Caesarean, but
less ordered. A table of substitutions is made up and used
to encrypt the information. Several different substitution
crypts may be applied in sequence, making the job of
breaking the code harder, even if the crypt tables are
available.

This coding method is, however highly vulnerable to letter
frequency analysis, as with the Caesaeran crypt above.

###### 4.4.3: Numeric Substitution crypt

In the Numeric Substitution crypt, a number replaces each
code group. Usually three digits, this code requires the
coding sheel to decrypt, particularly if multiple crypt
passes are employed or a rolling crypt system.

Decryption is by the inverse application of the system, as
with all strbstitution crypts.

This method of crypting is only partially vulnerable to
letter or group frequency analysis techniques.

###### 4.4.4: Logical Progression crypt

In the Logical Progression method of encryption, a set of
logical operations is carried out on each letter or letter
group, usually on each word.

Typical operations might be reversing the order of the
letters, or exchanging every other pair of letters.

Decryption is by the reverse application of the method.

Letter frequency analysis will always fail since no
substitution between letters has occurred (unless of
course, one of the logical operations is ae standard
substitution crypt), and intuitive analysis of the text
often results in a decrypt method faster than standard
exhaustive analysis techniques.

It should be noted that compound encryption schemes comprising
multiple passes of different crypt methods with different keys
are often used where more secure crypting 1s required without the
extra effort of using advanced techniques.

### 5: Additional Documentation

Psion Coropration document no 6100-0065
Organiser II Operating Manual

Psion Coropration document no 6100-0068
Organiser II Programming Manual

Psion Coropration document no 6100-0071
Organiser II Comms Link Manual
