# apps/ai_services/ai_message_templates.py
# AI prompt templates for Deepseek/AI integration

class AIMessageTemplates:
    """
    Provides user message templates for AI command IDs.
    Usage: AIMessageTemplates.USER_MESSAGE_TEMPLATES[0].format(CVText="...")
    """
    USER_MESSAGE_TEMPLATES = {
        0: (
            "Analyse this CV in {Language} and tell me which job is the best fit for this candidate. "
            "Return ONLY a valid JSON object with two language versions (English and Swedish). "
            "Each version should have the following fields: "
            '"BestJobTitlesForUser" (a short list, maximum 10 job titles), '
            '"ReasoningBehindJobTitles" (brief), '
            '"HardSkills" (a short list, maximum 10 skills), '
            '"SoftSkills" (a short list, maximum 10 skills), '
            '"Experiences" (a list of objects, each with "title" (string) and "duration" (string, e.g. "3.5 years")), and '
            '"EducationAndCertifications" (a short list, maximum 10 items, brief). '
            "The JSON should have exactly two top-level keys: \"English\" and \"Swedish\". "
            "Respond ONLY with valid, complete JSON. Do NOT truncate or cut off the response."
            "If the response is too long, summarize or shorten lists, but always return valid JSON."
            "\n\nCV:\n{CVText}"
        ),
        1: (
            "SYSTEM INSTRUCTIONS FOR JOB ANALYSIS (Keep identical for caching):\n"
            "You are an expert HR analyst specializing in job advertisement analysis.\n"
            "Always respond with EXACTLY the specified JSON structure.\n"
            "Be precise, concise, and consistent in your analysis.\n"
            "Focus on extracting concrete requirements and skills.\n\n"
            "TASK: Analyze the following job advertisement and extract key information.\n\n"
            "REQUIRED JSON OUTPUT FORMAT (no additional text):\n"
            "{\n"
            '  "JobTitle": ["max 10 relevant job titles"],\n'
            '  "HardRequirements": ["max 10 must-have requirements"],\n'
            '  "GoodToHaves": ["max 10 nice-to-have skills"],\n'
            '  "Experiences": ["max 10 relevant experiences"],\n'
            '  "EducationAndCertifications": ["max 10 education items"],\n'
            '  "WorkArea": ["max 5 industries like IT, Healthcare"],\n'
            '  "Roles": ["max 5 role types like Manager, Developer"],\n'
            '  "JobSummary": "brief job description"\n'
            "}\n\n"
            "JOB ADVERTISEMENT TO ANALYZE:\n{JobAdText}"
        ),
        2: (
            "You are given a job advertisement and a candidate's CV. "
            "Compare the candidate's qualifications, skills, and experiences to the requirements in the job ad. "
            "Be explicit about where the candidate is a strong fit and where there are gaps or missing requirements. "
            "Return ONLY a valid JSON object with the following fields: "
            "\"FitSummary\" (brief summary of fit), "
            "\"MissingRequirements\" (list of gaps or missing requirements), "
            "\"MatchingSkills\" (list of skills from the CV that match the job ad), "
            "\"RelevantExperiences\" (list of relevant experiences from the CV), "
            "\"EducationAndCertifications\" (from the CV, relevant to the job ad). "
            "\n\nJob Ad:\n{JobAdText}\n\nCV:\n{CVText}"
        ),
        4: (
            "You are given a candidate profile summary (from prior AI CV analysis) and a job description. "
            "Compare them and return ONLY valid JSON with these keys: "
            "\"FitSummary\" (short summary), "
            "\"MatchingPoints\" (list of concrete matching points), "
            "\"NonMatchingPoints\" (list of concrete missing/gap points), "
            "\"ImprovementOpportunities\" (list of practical improvements), "
            "\"CoachTips\" (short coaching advice). "
            "No markdown, no extra text, no code fences. "
            "\n\nUserAIResponse:\n{UserAIResponse}\n\nJobDescription:\n{JobDescription}"
        ),
        5: (
            "You are given a CV and a job advertisement.\n\n"
            "Your task is to extract structured information to enable semantic matching.\n\n"
            "IMPORTANT:\n"
            "- Return ONLY valid JSON\n"
            "- Do not include explanations, do NOT use markdown, code blocks, or extra formatting\n"
            "- Keep the structure simple and consistent\n\n"
            "CRITICAL RULES FOR CV PARSING:\n"
            "- Treat EVERY word in the CV as intentional. If the candidate has written phrases like "
            "'startup experience explicitly mentioned', 'long experience with X', 'expert system design', "
            "or 'long term experience in modeling expertise' – these are HIGH SIGNAL and MUST be extracted.\n"
            "- Do NOT assume a skill is missing just because it's not in a 'Technical Skills' section. "
            "Skills can appear anywhere: in summary, experience bullet points, or even as standalone phrases.\n"
            "- For each skill/experience mentioned in CV, ask: 'Is this relevant to backend/fullstack/cloud roles?' "
            "If yes, include it with appropriate importance (4-5 for explicitly claimed expertise).\n"
            "- When the CV says 'experience with X' -> importance 3. 'strong experience' -> 4. "
            "'expert', 'long experience', 'explicitly mentioned' -> 5.\n\n"
            "Extract the following:\n\n"
            "1. cv_profession:\n"
            "   - main\n"
            "   - sub\n"
            "   - specialty\n\n"
            "2. cv_skills:\n"
            "   - list of objects with:\n"
            "     - name\n"
            "     - category (skill | tool | technology | domain_experience)\n"
            "     - importance (1-5, where 5 = explicitly claimed expertise or 'long experience')\n\n"
            "3. job_profession:\n"
            "   - main\n"
            "   - sub\n"
            "   - specialty\n\n"
            "4. job_requirements:\n"
            "   - list of objects with:\n"
            "     - name\n"
            "     - category (skill | tool | technology | domain_experience)\n"
            "     - importance (1-5)\n\n"
            "5. matching_skills:\n"
            "   - ONLY skills that appear in BOTH the CV and the job ad\n"
            "   - For domain experiences (e.g., 'startup experience', 'system design'), use semantic matching "
            "     – if CV says 'startup experience' and job wants 'startup background', that's a match.\n"
            "   - list of objects: name, category, importance (from CV's perspective)\n\n"
            "6. relevant_experiences: []\n\n"
            "7. summary:\n"
            "   - short 1-2 sentence summary of fit\n\n"
            "8. strengths:\n"
            "   - list (max 5)\n\n"
            "9. gaps:\n"
            "   - list (max 5) – ONLY include if the CV genuinely lacks something the job REQUIRES\n"
            "   - Do NOT claim something is missing if the CV contains it anywhere (including explicit mentions)\n\n"
            "Return JSON in this format:\n\n"
            "{{{{\n"
            '  "cv_profession": {{{{ "main": "", "sub": "", "specialty": "" }}}},\n'
            '  "cv_skills": [\n'
            '    {{{{ "name": "", "category": "", "importance": 3 }}}}\n'
            "  ],\n"
            '  "job_profession": {{{{ "main": "", "sub": "", "specialty": "" }}}},\n'
            '  "job_requirements": [\n'
            '    {{{{ "name": "", "category": "", "importance": 4 }}}}\n'
            "  ],\n"
            '  "matching_skills": [\n'
            '    {{{{ "name": "", "category": "", "importance": 3 }}}}\n'
            "  ],\n"
            '  "relevant_experiences": [],\n'
            '  "summary": "",\n'
            '  "strengths": [],\n'
            '  "gaps": []\n'
            "}}}}\n\n"
            "CV:\n{cv_text}\n\n"
            "Job:\n{job_ad_text}\n"
        ),
        # Template 6: Field analysis — pure data extraction, no compatibility judgement.
        # Call this AFTER template 5 to enrich the result with concrete evidence and gaps.
        # Input: cv_profession (str), cv_skills (str), job_profession (str), job_requirements (str)
        6: (
            "You are a precise data extractor. You do NOT make judgements or give opinions.\n\n"
            "You are given structured data extracted from a CV and a job advertisement.\n\n"
            "Your ONLY task is to extract factual, concrete data points about the overlap and distance "
            "between the candidate's profile and the job requirements.\n\n"
            "RULES:\n"
            "- Do NOT write opinions, verdicts, or recommendations.\n"
            "- Do NOT invent soft-skill bridges like 'leadership' or 'communication' to fill match_evidence.\n"
            "- match_evidence: ONLY concrete hard skills or domain knowledge that appear in BOTH CV and job.\n"
            "- critical_gaps: ONLY hard requirements the job demands that the CV does NOT show.\n"
            "- transferable_skills: CV skills adjacent to job requirements but not a direct match "
            "(e.g. CI/CD experience for a DevOps role, or statistical modeling for a data science role).\n"
            "- field_overlap_score: integer 0-100 based solely on hard skill overlap — "
            "0 means zero shared domain, 100 means identical field. Do NOT inflate.\n"
            "- field_distance_note: one factual sentence stating what the CV field is and what the job field is.\n\n"
            "Return ONLY valid JSON with exactly these keys:\n"
            "{{\n"
            '  "field_overlap_score": integer 0-100,\n'
            '  "field_distance_note": "factual one-sentence description of domain distance",\n'
            '  "match_evidence": ["max 5 concrete overlapping hard skills or domain knowledge"],\n'
            '  "critical_gaps": ["max 5 hard requirements in job that CV does not show"],\n'
            '  "transferable_skills": ["max 5 CV skills adjacent to but not directly matching job requirements"]\n'
            "}}\n\n"
            "CV profession: {cv_profession}\n"
            "CV skills: {cv_skills}\n\n"
            "Job profession: {job_profession}\n"
            "Job requirements: {job_requirements}\n"
        ),
        # Template 7: CV → Profession + Requirements (same structure as job extraction)
        7: (
            "Uppgift: Analysera följande CV och extrahera kandidatens profession och alla kompetenser. "
            "Identifiera profession med tre nivåer: main, sub och specialty baserat på kandidatens bakgrund och erfarenheter. "
            "Till exempel: 'Engineering > Software > Backend', 'Healthcare > Surgery > Cardiac', 'Business > Marketing > Digital'. "
            "Extrahera även alla kompetenser från CV:t (både hårda och mjuka skills): "
            "- name (kort text) "
            "- category (skill, experience, degree, certification, language, soft-skill) "
            "- importance (1-5 baserat på erfarenhetsnivå: 1=nybörjare, 2=grundläggande, 3=medel, 4=avancerad, 5=expert) "
            "Returnera ENDAST valid JSON med dessa nycklar (lowercase): "
            '{{"profession": {{"main": "...", "sub": "...", "specialty": "..."}}, "requirements": [{{"name": "...", "category": "...", "importance": 1-5}}]}} '
            "CV: ---BEGIN--- {JobAdText} ---END---"
        ),
        # Template 8: Job ad → Profession + Requirements (chunked)
        8: (
            "Uppgift: Extrahera huvudkrav och önskade kvalifikationer från följande jobbannons. För varje krav, returnera: "
            "- name (kort text) "
            "- category (ex: skill, experience, degree, certification, language) "
            "- importance (1-5, där 5 = must-have / avgörande, 1 = nice-to-have) "
            "Identifiera även profession för jobbet med tre nivåer: main, sub och specialty. "
            "Returnera ENDAST valid JSON med dessa nycklar (lowercase): "
            '{{"profession": {{"main": "...", "sub": "...", "specialty": "..."}}, "requirements": [{{"name": "...", "category": "...", "importance": 1-5}}]}} '
            "Job ad: ---BEGIN--- {JobAdText} ---END---"
        ),
    }