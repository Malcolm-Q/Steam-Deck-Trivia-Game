using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Web;
using UnityEngine.UI;
using System.Linq;

public class uiController : MonoBehaviour
{
    // you can make these private and .Find these if you don't like inspector clutter.
    public GameObject transition, mainMenu, customMenu, mainUI,trueFalse,multipleChoice,threePathsChoice, livesObj;
    public Animator tranAnim, countAnim, but1, but2, but3, but4,trueAnim,falseAnim,pointsAnim,recapAnim;
    public Text question;
    public Text ans1, ans2, ans3, ans4, countDownText, pointsText, numQuestionsText, diffText, categoriesText, livesText;

    // vars for stats and making the game work.
    private Text[] answerArray;
    private Animator[] buttons;
    private string correct_answer;
    private TriviaData triviaData;
    private int points = 0, wonPoints = 0, roundsWon = 0, roundsPlayed=0, current_question = 0, lives=3;
    private List<float> reactionTime = new List<float>();
    private List<string> categoriesToChoose = new List<string>();
    private float countDown = 15.9f;
    private bool answered = false, suddenDeath=false, count = false, threePathsBool = false;

    // vars for custom API calls.
    private string categoryUrl = "&category=", rounds = "10", diffUrl = "&difficulty=", customURL = "https://opentdb.com/api.php?amount=";
    private string[] numQuestions = {"5","10","15","20","30","40","50"};
    private string[] diff = {"mixed","easy","medium","hard"};
    private string[] categories = {"All","General Knowledge","Books","Film","Music","Musicals","Television","Video Games","Board Games","Nature","Computers","Mathematics","Mythology","Sports","Geography","History","Politics","Art","Celebrities","Animals","Vehicles","Comics","Gadgets","Anime","Cartoons"};
    private string[] categoriesInt = {"-1","9","10","11","12","13","14","15","16","17","18","19","20","21","22","23","24","25","26","27","28","29","30","31","32"};
    private int selectedDiff = 0, selectedNumQuestions = 1,selectedCategory = 0;

    private void Start()
    {
        Cursor.visible = false;
        answerArray = new Text[] { ans1, ans2, ans3, ans4 };
        buttons = new Animator[] { but1, but2, but3, but4 };
    }

    private void Update()
    {
        if(count)
        {
            countDown -= Time.deltaTime;
            countDownText.text = Mathf.Floor(countDown).ToString();
            if(countDown <= 1f)
            {
                // TryAnswer() is set to receive -1 as no answer.
                TryAnswer(-1);
                countDown = 0f;
                count = false;
            }
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        current_question = 0;
        points = 0;
        rounds="10";
        livesObj.SetActive(false);
        suddenDeath = false;
        StartCoroutine(QuickStart());
    }

    public void StartThreePaths()
    {
        current_question = 0;
        points = 0;
        rounds="3";
        lives = 3;
        livesObj.SetActive(true);
        threePathsBool = true;
        suddenDeath = false;
        StartCoroutine(PresentPaths());
    }

    private IEnumerator PresentPaths(bool wait = false)
    {
        if(wait) yield return new WaitForSeconds(3);
        categoriesToChoose.Clear();
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);
        multipleChoice.SetActive(false);
        trueFalse.SetActive(false);
        mainMenu.SetActive(false);
        customMenu.SetActive(false);
        mainUI.SetActive(true);
        threePathsChoice.SetActive(true);
        question.text = "Pick a Category";

        // simple way to grab random indices without stepping on toes.
        var cat1 = UnityEngine.Random.Range(1,categories.Count()-1);
        categoriesToChoose.Add(categoriesInt[cat1]);

        var cat2 = UnityEngine.Random.Range(1,categories.Count()-1);
        while(cat2 == cat1) cat2 = UnityEngine.Random.Range(1,categories.Count()-1);
        categoriesToChoose.Add(categoriesInt[cat2]);

        var cat3 = UnityEngine.Random.Range(1,categories.Count()-1);
        while(cat3 == cat2 || cat3 == cat1) cat3 = UnityEngine.Random.Range(1,categories.Count()-1);
        categoriesToChoose.Add(categoriesInt[cat3]);

        threePathsChoice.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = categories[cat1];
        threePathsChoice.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = categories[cat2];
        threePathsChoice.transform.GetChild(2).GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = categories[cat3];
        livesText.text = "Lives: " + lives.ToString();
        current_question = 0;

        tranAnim.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1);
    }

    public void ChoosePath(int path)
    {
        var url = "https://opentdb.com/api.php?amount=3&category=" + categoriesToChoose[path];
        threePathsChoice.transform.GetChild(path).GetComponent<Animator>().SetTrigger("wiggle");
        StartCoroutine(QuickStart(url));
    }

    private IEnumerator QuickStart(string url = "https://opentdb.com/api.php?amount=10")
    {
        countDown = 15.9f;

        //begin transition
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);

        mainMenu.SetActive(false);
        customMenu.SetActive(false);
        threePathsChoice.SetActive(false);
        mainUI.SetActive(true);

        //make request while in between screens
        yield return StartCoroutine(APIManager.GetDataFromApi(url,result => { triviaData = result; }));

        //prep game
        question.text = HttpUtility.HtmlDecode(triviaData.results[0].question);
        correct_answer = HttpUtility.HtmlDecode(triviaData.results[0].correct_answer);
        List<string> shuffledAnswers = ShuffleAnswers(triviaData.results[0]);

        if(triviaData.results[0].type == "multiple")
        {
            trueFalse.SetActive(false);
            multipleChoice.SetActive(true);

            for (int i = 0; i < shuffledAnswers.Count;i++)
            {
                answerArray[i].text = HttpUtility.HtmlDecode(shuffledAnswers[i]);
            }
        }
        else
        {
            multipleChoice.SetActive(false);
            trueFalse.SetActive(true);
        }
        //end transition
        tranAnim.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1);
        count = true;
        answered = false;
    }

    public void TryAnswer(int question_num)
    {
        if(answered)return;

        // -1 is if the user runs out of time and fails to answer.
        if(question_num == -1)
        {
            answered = true;
            count = false;
            countAnim.enabled=false;
            //play animations to reveal answers.
            if(triviaData.results[current_question].type == "multiple")
            {
                for(int i=0; i < 4; i++)
                {
                    if(HttpUtility.HtmlDecode(answerArray[i].text) == correct_answer) buttons[i].SetTrigger("green");
                    else buttons[i].SetTrigger("red");
                }
            }
            else
            {
                if(correct_answer == "False")
                {
                    falseAnim.SetTrigger("green");
                    trueAnim.SetTrigger("red");
                }
                else
                {
                    falseAnim.SetTrigger("red");
                    trueAnim.SetTrigger("green");
                }
            }
            if(suddenDeath){
                StartCoroutine(EndSuddenDeath()); 
                return;
            }
            //Handle next.
            current_question += 1;
            lives--;
            livesText.text = "Lives: " + lives.ToString();
            if(threePathsBool  && lives < 1)
            {
                StartCoroutine(EndSuddenDeath());
                return;
            }
            if(current_question < triviaData.results.Count) StartCoroutine(NextQuestion());
            else if(threePathsBool) StartCoroutine(PresentPaths());
            else if(suddenDeath) StartCoroutine(QuickStart("https://opentdb.com/api.php?amount=50"));
            else StartCoroutine(EndGame());
            return;
        }
        answered = true;
        count = false;
        countAnim.enabled=false;
        buttons[question_num-1].transform.parent.gameObject.GetComponent<Animator>().SetTrigger("wiggle");
        if(triviaData.results[current_question].type == "multiple")
        {
            for(int i=0; i < 4; i++)
            {
                if(HttpUtility.HtmlDecode(answerArray[i].text) == correct_answer) buttons[i].SetTrigger("green");
                else buttons[i].SetTrigger("red");
            }
            if(HttpUtility.HtmlDecode(answerArray[question_num-1].text) == correct_answer)
            {
                // calculate points earned if correct answer.
                wonPoints = (int)Mathf.Round(150 * countDown);
            }
            else if(suddenDeath) StartCoroutine(EndSuddenDeath());
            else{
                lives--;
                livesText.text = "Lives: " + lives.ToString();
            }
            if(threePathsBool && lives < 1)
            {
                StartCoroutine(EndSuddenDeath());
                return;
            }
        }
        else
        {
            // handle true/false.
            if(question_num == 1)
            {
                if(correct_answer == "False")
                {
                    wonPoints = (int)Mathf.Round(150 * countDown);
                    falseAnim.SetTrigger("green");
                    trueAnim.SetTrigger("red");
                }
                else
                {
                    lives--;
                    livesText.text = "Lives: " + lives.ToString();
                    falseAnim.SetTrigger("red");
                    trueAnim.SetTrigger("green");
                    if(suddenDeath)
                    {
                        StartCoroutine(EndSuddenDeath());
                        return;
                    }
                    else if(threePathsBool  && lives < 1)
                    {
                        StartCoroutine(EndSuddenDeath());
                        return;
                    }
                }
            }
            else
            {
                if(correct_answer == "True")
                {
                    wonPoints = (int)Mathf.Round(150 * countDown);
                    trueAnim.SetTrigger("green");
                    falseAnim.SetTrigger("red");
                }
                else
                {
                    lives--;
                    livesText.text = "Lives: " + lives.ToString();
                    trueAnim.SetTrigger("red");
                    falseAnim.SetTrigger("green");
                    if(suddenDeath)
                    {
                        StartCoroutine(EndSuddenDeath());
                        return;
                    }
                    else if(threePathsBool  && lives < 1)
                    {
                        StartCoroutine(EndSuddenDeath());
                        return;
                    }
                }
            }
        }
        current_question += 1;
        if(current_question < triviaData.results.Count) StartCoroutine(NextQuestion());
        else{
            if(threePathsBool)StartCoroutine(PresentPaths(true));
            else if(!suddenDeath) StartCoroutine(EndGame());
            else StartCoroutine(QuickStart("https://opentdb.com/api.php?amount=50"));
        }
    }


    // These three voids are just so buttons can start coroutines.
    public void SuddenDeath()
    {
        suddenDeath = true;
        livesObj.SetActive(false);
        StartCoroutine(QuickStart("https://opentdb.com/api.php?amount=50"));
    }

    public void OpenCustomMenu()
    {
        StartCoroutine(CustomGame());
    }

    public void CloseCustomMenu()
    {
        StartCoroutine(CloseCustomGame());
    }


    // These next voids are all for creating a custom game.
    public void CycleDiff()
    {
        selectedDiff++;
        if(selectedDiff == diff.Count()) selectedDiff = 0;
        diffText.text = diff[selectedDiff];
    }

    public void CycleRounds()
    {
        selectedNumQuestions++;
        if (selectedNumQuestions == numQuestions.Count()) selectedNumQuestions = 0;
        numQuestionsText.text = numQuestions[selectedNumQuestions];
    }

    public void CycleCategory()
    {
        selectedCategory++;
        if(selectedCategory == categories.Count()) selectedCategory = 0;
        categoriesText.text = categories[selectedCategory];
    }
    public void ResetCustomGame()
    {
        selectedCategory = 0;
        categoriesText.text = categories[selectedCategory];

        selectedNumQuestions = 1;
        numQuestionsText.text = numQuestions[selectedNumQuestions];

        selectedDiff = 0;
        diffText.text = diff[selectedDiff];
    }

    public void StartCustomGame()
    {
        livesObj.SetActive(false);
        var url = customURL + numQuestions[selectedNumQuestions];
        rounds = numQuestions[selectedNumQuestions];

        if(categories[selectedCategory] != "All") url += categoryUrl + categoriesInt[selectedCategory];
        if(diff[selectedDiff] != "mixed") url += diffUrl + diff[selectedDiff];
        suddenDeath = false;
        StartCoroutine(QuickStart(url));
    }

    private IEnumerator CloseCustomGame()
    {
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);
        mainMenu.SetActive(true);
        customMenu.SetActive(false);
        tranAnim.SetTrigger("FadeOut");
    }

    private IEnumerator CustomGame()
    {
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);
        mainMenu.SetActive(false);
        customMenu.SetActive(true);
        tranAnim.SetTrigger("FadeOut");
    }

    // if user is in suddenDeath gamemode and fails a question this executes.
    private IEnumerator EndSuddenDeath()
    {
        // stop clock and give a buffer for the user to read the answers.
        count=false;
        yield return new WaitForSeconds(3);

        // prep the 'recap' screen that displays your stats for that game.
        var recap = GameObject.Find("recap");
        var avg_time = 0f;
        try {avg_time = reactionTime.Average();}
        catch {avg_time = 15f;}
        recap.transform.GetChild(0).gameObject.GetComponent<Text>().text = roundsWon.ToString() + "\nCorrect Answers";
        recap.transform.GetChild(1).gameObject.GetComponent<Text>().text = points.ToString()+"\nPoints";
        recap.transform.GetChild(2).gameObject.GetComponent<Text>().text = avg_time.ToString("F2")+"\nAvg Reaction Time";

        // now that it's prepped we can transition to it.
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);
        mainUI.SetActive(false);
        tranAnim.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1);

        // start recap animation.
        recapAnim.SetTrigger("start");
        yield return new WaitForSeconds(6.75f);

        // transition out.
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1);
        mainMenu.SetActive(true);
        tranAnim.SetTrigger("FadeOut");

        // reset game states.
        answered = false;
        reactionTime.Clear();
        current_question = 0;
        points = 0;
        wonPoints = 0;
        roundsWon = 0;
        suddenDeath = false;
        threePathsBool = false;
    }

    private IEnumerator EndGame()
    {
        // same as above more or less.
        count=false;
        yield return new WaitForSeconds(3);

        if(wonPoints > 0) 
        {
            // if user answered correctly play animation adding points.
            pointsAnim.SetTrigger("play");
            pointsText.text = "+" + wonPoints.ToString();
            points += wonPoints;
            wonPoints = 0;
            roundsWon++;
            yield return new WaitForSeconds(1.5f);
        }

        var recap = GameObject.Find("recap");
        var avg_time = reactionTime.Average();
        recap.transform.GetChild(0).gameObject.GetComponent<Text>().text = roundsWon.ToString() + "/" + rounds + "\nCorrect Answers";
        recap.transform.GetChild(1).gameObject.GetComponent<Text>().text = points.ToString()+"\nPoints";
        recap.transform.GetChild(2).gameObject.GetComponent<Text>().text = avg_time.ToString("F2")+"\nAvg Reaction Time";

        // begin transition
        tranAnim.SetTrigger("FadeIn");
        
        roundsPlayed++;
        yield return new WaitForSeconds(1f);
        mainUI.SetActive(false);
        tranAnim.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1f);

        // start recap anim again.
        recapAnim.SetTrigger("start");
        yield return new WaitForSeconds(6.75f);

        // transition out
        tranAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1f);
        mainMenu.SetActive(true);
        tranAnim.SetTrigger("FadeOut");
        answered = false;
        reactionTime.Clear();
        current_question = 0;
        points = 0;
        roundsWon = 0;
        threePathsBool = false;
        suddenDeath = false;
    }

    private IEnumerator NextQuestion()
    {
        // yield so user can digest answers then transition.
        yield return new WaitForSeconds(3);
        tranAnim.SetTrigger("FadeIn");
        if(wonPoints > 0)
        {
            pointsAnim.SetTrigger("play");
            pointsText.text = "+" + wonPoints.ToString();
            points += wonPoints;
            wonPoints = 0;
            roundsWon++;
            yield return new WaitForSeconds(1.5f);
        }
        yield return new WaitForSeconds(1f);
        roundsPlayed++;

        // add users reaction time to list so it can be averaged later.
        reactionTime.Add((15.9f - countDown));
        countDown = 15.9f;
        countDownText.text = "15";

        // prep next question.
        question.text = HttpUtility.HtmlDecode(triviaData.results[current_question].question);
        correct_answer = HttpUtility.HtmlDecode(triviaData.results[current_question].correct_answer);
        List<string> shuffledAnswers = ShuffleAnswers(triviaData.results[current_question]);

        if(triviaData.results[current_question].type == "multiple")
        {
            trueFalse.SetActive(false);
            multipleChoice.SetActive(true);

            for (int i = 0; i < shuffledAnswers.Count;i++)
            {
                answerArray[i].text = HttpUtility.HtmlDecode(shuffledAnswers[i]);
            }
        }
        else
        {
            multipleChoice.SetActive(false);
            trueFalse.SetActive(true);
        }
        
        // finish transition and reset countdown animation.
        tranAnim.SetTrigger("FadeOut");
        yield return new WaitForSeconds(1);
        answered = false;
        count = true;
        countAnim.enabled=true;
        countAnim.Play("countdown_bump", 0, 0f);
        countAnim.Update(0f);
    }

    public static List<string> ShuffleAnswers(TriviaQuestion question)
    {
        List<string> answers = new List<string>();
        answers.Add(question.correct_answer);
        answers.AddRange(question.incorrect_answers);

        // simple fisher yates shuffle
        for (int i = 0; i < answers.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, answers.Count);
            string temp = answers[i];
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temp;
        }
        return answers;
    }
}


