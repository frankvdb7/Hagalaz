export interface NewsItem {
    id: string;
    title: string;
    author: string;
    created: Date;
    content: string;
    image: string;
    tags: string[];
}

export const newsItems: NewsItem[] = [
    {
        id: "1",
        title: "Welcome to Hagalaz",
        content: `
Welcome to the world of Hagalaz, where adventure awaits at every corner. Our team has been working tirelessly to bring you an immersive experience filled with exciting quests, challenging dungeons, and a vibrant community.

### Features

- **Epic Quests**: Embark on thrilling quests that will test your skills and bravery.
- **Stunning Graphics**: Enjoy breathtaking visuals that bring the world of Hagalaz to life.
- **Community Events**: Participate in regular events and activities to earn exclusive rewards.

### Join Us

Create your character today and join thousands of players in the ultimate fantasy adventure. Whether you're a seasoned veteran or a newcomer, there's something for everyone in Hagalaz.

See you in the game!

**Hagalaz Team**
        `,
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-1.jpg",
        tags: ["update", "release", "event"],
    },
    {
        id: "2",
        title: "News Item 1",
        content: "The content for news item 1",
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-2.jpg",
        tags: ["patch", "bugfix", "maintenance"],
    },
    {
        id: "3",
        title: "News Item 2",
        content: "The content for news item 2",
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-3.jpg",
        tags: ["feature", "expansion", "content"],
    },
    {
        id: "4",
        title: "News Item 3",
        content: "The content for news item 3",
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-4.jpg",
        tags: ["hotfix", "balance", "improvement"],
    },
    {
        id: "5",
        title: "News Item 4",
        content: "The content for news item 4",
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-5.jpg",
        tags: ["update", "content", "event"],
    },
    {
        id: "6",
        title: "News Item 5",
        content: "The content for news item 5",
        author: "Storm",
        created: new Date(),
        image: "./assets/images/slideshow/wallpapers-6.jpg",
        tags: ["patch", "feature", "improvement"],
    },
];
