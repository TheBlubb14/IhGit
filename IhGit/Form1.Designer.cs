namespace IhGit
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            buttonClear = new Button();
            textBoxFeatureName = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBoxCommits = new TextBox();
            buttonUpmerge = new Button();
            textBoxDescription = new TextBox();
            label3 = new Label();
            label4 = new Label();
            textBoxTitle = new TextBox();
            textBoxOutput = new TextBox();
            buttonPullRequest = new Button();
            buttonFetch = new Button();
            buttonPush = new Button();
            checkBoxDryRun = new CheckBox();
            buttonNewBranch = new Button();
            buttonPullRequestMultiple = new Button();
            checkBoxStartOnSameVersion = new CheckBox();
            textBoxUserName = new TextBox();
            label5 = new Label();
            label6 = new Label();
            textBoxRepo = new TextBox();
            buttonDaniel = new Button();
            buttonSpike = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(634, 483);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Status";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(1061, 483);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(75, 23);
            buttonClear.TabIndex = 2;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // textBoxFeatureName
            // 
            textBoxFeatureName.Location = new Point(131, 7);
            textBoxFeatureName.Name = "textBoxFeatureName";
            textBoxFeatureName.Size = new Size(578, 23);
            textBoxFeatureName.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(81, 15);
            label1.TabIndex = 4;
            label1.Text = "Feature Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 39);
            label2.Name = "label2";
            label2.Size = new Size(56, 15);
            label2.TabIndex = 6;
            label2.Text = "Commits";
            // 
            // textBoxCommits
            // 
            textBoxCommits.Location = new Point(131, 36);
            textBoxCommits.Multiline = true;
            textBoxCommits.Name = "textBoxCommits";
            textBoxCommits.Size = new Size(578, 203);
            textBoxCommits.TabIndex = 5;
            // 
            // buttonUpmerge
            // 
            buttonUpmerge.Location = new Point(131, 483);
            buttonUpmerge.Name = "buttonUpmerge";
            buttonUpmerge.Size = new Size(75, 23);
            buttonUpmerge.TabIndex = 7;
            buttonUpmerge.Text = "Upmerge";
            buttonUpmerge.UseVisualStyleBackColor = true;
            buttonUpmerge.Click += buttonUpmerge_Click;
            // 
            // textBoxDescription
            // 
            textBoxDescription.Location = new Point(131, 274);
            textBoxDescription.Multiline = true;
            textBoxDescription.Name = "textBoxDescription";
            textBoxDescription.Size = new Size(578, 203);
            textBoxDescription.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 277);
            label3.Name = "label3";
            label3.Size = new Size(67, 15);
            label3.TabIndex = 9;
            label3.Text = "Description";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 253);
            label4.Name = "label4";
            label4.Size = new Size(29, 15);
            label4.TabIndex = 11;
            label4.Text = "Title";
            // 
            // textBoxTitle
            // 
            textBoxTitle.Location = new Point(131, 245);
            textBoxTitle.Name = "textBoxTitle";
            textBoxTitle.Size = new Size(578, 23);
            textBoxTitle.TabIndex = 10;
            // 
            // textBoxOutput
            // 
            textBoxOutput.Location = new Point(715, 7);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.Size = new Size(421, 470);
            textBoxOutput.TabIndex = 12;
            // 
            // buttonPullRequest
            // 
            buttonPullRequest.Location = new Point(516, 483);
            buttonPullRequest.Name = "buttonPullRequest";
            buttonPullRequest.Size = new Size(112, 23);
            buttonPullRequest.TabIndex = 13;
            buttonPullRequest.Text = "Pull Request";
            buttonPullRequest.UseVisualStyleBackColor = true;
            buttonPullRequest.Click += buttonPullRequest_Click;
            // 
            // buttonFetch
            // 
            buttonFetch.Location = new Point(212, 483);
            buttonFetch.Name = "buttonFetch";
            buttonFetch.Size = new Size(75, 23);
            buttonFetch.TabIndex = 14;
            buttonFetch.Text = "Fetch";
            buttonFetch.UseVisualStyleBackColor = true;
            buttonFetch.Click += buttonFetch_Click;
            // 
            // buttonPush
            // 
            buttonPush.Location = new Point(293, 483);
            buttonPush.Name = "buttonPush";
            buttonPush.Size = new Size(75, 23);
            buttonPush.TabIndex = 15;
            buttonPush.Text = "Push";
            buttonPush.UseVisualStyleBackColor = true;
            buttonPush.Click += buttonPush_Click;
            // 
            // checkBoxDryRun
            // 
            checkBoxDryRun.AutoSize = true;
            checkBoxDryRun.Location = new Point(374, 487);
            checkBoxDryRun.Name = "checkBoxDryRun";
            checkBoxDryRun.Size = new Size(68, 19);
            checkBoxDryRun.TabIndex = 16;
            checkBoxDryRun.Text = "Dry Run";
            checkBoxDryRun.UseVisualStyleBackColor = true;
            // 
            // buttonNewBranch
            // 
            buttonNewBranch.Location = new Point(131, 512);
            buttonNewBranch.Name = "buttonNewBranch";
            buttonNewBranch.Size = new Size(111, 23);
            buttonNewBranch.TabIndex = 17;
            buttonNewBranch.Text = "New Branch";
            buttonNewBranch.UseVisualStyleBackColor = true;
            buttonNewBranch.Click += buttonNewBranch_Click;
            // 
            // buttonPullRequestMultiple
            // 
            buttonPullRequestMultiple.Location = new Point(516, 512);
            buttonPullRequestMultiple.Name = "buttonPullRequestMultiple";
            buttonPullRequestMultiple.Size = new Size(193, 23);
            buttonPullRequestMultiple.TabIndex = 18;
            buttonPullRequestMultiple.Text = "Pull Request Multiple";
            buttonPullRequestMultiple.UseVisualStyleBackColor = true;
            buttonPullRequestMultiple.Click += buttonPullRequestMultiple_Click;
            // 
            // checkBoxStartOnSameVersion
            // 
            checkBoxStartOnSameVersion.AutoSize = true;
            checkBoxStartOnSameVersion.Location = new Point(516, 541);
            checkBoxStartOnSameVersion.Name = "checkBoxStartOnSameVersion";
            checkBoxStartOnSameVersion.Size = new Size(139, 19);
            checkBoxStartOnSameVersion.TabIndex = 19;
            checkBoxStartOnSameVersion.Text = "Start on same version";
            checkBoxStartOnSameVersion.UseVisualStyleBackColor = true;
            // 
            // textBoxUserName
            // 
            textBoxUserName.Location = new Point(131, 570);
            textBoxUserName.Name = "textBoxUserName";
            textBoxUserName.Size = new Size(311, 23);
            textBoxUserName.TabIndex = 20;
            textBoxUserName.Text = "TheBlubb14";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 578);
            label5.Name = "label5";
            label5.Size = new Size(60, 15);
            label5.TabIndex = 21;
            label5.Text = "Username";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 607);
            label6.Name = "label6";
            label6.Size = new Size(34, 15);
            label6.TabIndex = 23;
            label6.Text = "Repo";
            // 
            // textBoxRepo
            // 
            textBoxRepo.Location = new Point(131, 599);
            textBoxRepo.Name = "textBoxRepo";
            textBoxRepo.Size = new Size(311, 23);
            textBoxRepo.TabIndex = 22;
            textBoxRepo.Text = "C:\\Dev\\Projects\\GitHub\\paxcontrol";
            // 
            // buttonDaniel
            // 
            buttonDaniel.Location = new Point(131, 628);
            buttonDaniel.Name = "buttonDaniel";
            buttonDaniel.Size = new Size(75, 23);
            buttonDaniel.TabIndex = 24;
            buttonDaniel.Text = "Daniel";
            buttonDaniel.UseVisualStyleBackColor = true;
            buttonDaniel.Click += buttonDaniel_Click;
            // 
            // buttonSpike
            // 
            buttonSpike.Location = new Point(212, 628);
            buttonSpike.Name = "buttonSpike";
            buttonSpike.Size = new Size(75, 23);
            buttonSpike.TabIndex = 25;
            buttonSpike.Text = "Spike";
            buttonSpike.UseVisualStyleBackColor = true;
            buttonSpike.Click += buttonSpike_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1148, 722);
            Controls.Add(buttonSpike);
            Controls.Add(buttonDaniel);
            Controls.Add(label6);
            Controls.Add(textBoxRepo);
            Controls.Add(label5);
            Controls.Add(textBoxUserName);
            Controls.Add(checkBoxStartOnSameVersion);
            Controls.Add(buttonPullRequestMultiple);
            Controls.Add(buttonNewBranch);
            Controls.Add(checkBoxDryRun);
            Controls.Add(buttonPush);
            Controls.Add(buttonFetch);
            Controls.Add(buttonPullRequest);
            Controls.Add(textBoxOutput);
            Controls.Add(label4);
            Controls.Add(textBoxTitle);
            Controls.Add(label3);
            Controls.Add(textBoxDescription);
            Controls.Add(buttonUpmerge);
            Controls.Add(label2);
            Controls.Add(textBoxCommits);
            Controls.Add(label1);
            Controls.Add(textBoxFeatureName);
            Controls.Add(buttonClear);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button buttonClear;
        private TextBox textBoxFeatureName;
        private Label label1;
        private Label label2;
        private TextBox textBoxCommits;
        private Button buttonUpmerge;
        private TextBox textBoxDescription;
        private Label label3;
        private Label label4;
        private TextBox textBoxTitle;
        private TextBox textBoxOutput;
        private Button buttonPullRequest;
        private Button buttonFetch;
        private Button buttonPush;
        private CheckBox checkBoxDryRun;
        private Button buttonNewBranch;
        private Button buttonPullRequestMultiple;
        private CheckBox checkBoxStartOnSameVersion;
        private TextBox textBoxUserName;
        private Label label5;
        private Label label6;
        private TextBox textBoxRepo;
        private Button buttonDaniel;
        private Button buttonSpike;
    }
}